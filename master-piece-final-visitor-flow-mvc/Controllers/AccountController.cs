using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services.Email;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    IAccountEmailSender emailSender, TimeProvider clock, AccountEmailLinks emailLinks,
    AccountEmailCooldown cooldown) : Controller
{
    [HttpGet("/login.html")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("/login.html")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Confirm your email before signing in.");
            return View(model);
        }

        if (user.Status != AccountStatus.Active)
        {
            ModelState.AddModelError(string.Empty, "This account is not active.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            // إذا كان الرابط المطلوب داخلياً وآمناً نرجع إليه.
            if (!user.IsProfileComplete && await userManager.IsInRoleAsync(user, "Member"))
            {
                foreach (var period in Enum.GetValues<BillingPeriod>())
                {
                    if (model.ReturnUrl != $"/subscription-checkout.html?period={period}") continue;
                    TempData["MembershipPeriod"] = (int)period;
                    return RedirectToAction(nameof(ProfileSetup));
                }
            }

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl!);
            }

            // نفس صفحة الدخول، لكن الأدمن ينتقل إلى لوحة تحكم الأدمن.
            if (await userManager.IsInRoleAsync(user, "Admin"))
            {
                return Redirect("/Admin/admin-html/dashboard.html");
            }

            // المستخدم العادي يجب أن يكمل ملفه أولاً.
            if (!user.IsProfileComplete)
            {
                return RedirectToAction(
                    actionName: "ProfileSetup",
                    controllerName: "Account"
                );
            }

            return RedirectToAction(
                actionName: "Dashboard",
                controllerName: "Account"
            );
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Account temporarily locked after repeated failed attempts."
            : "Invalid email or password.");
        return View(model);
    }

    [HttpGet("/signup.html")]
    [AllowAnonymous]
    public IActionResult Signup(BillingPeriod? membershipPeriod = null) =>
        View(new SignupViewModel { MembershipPeriod = membershipPeriod is { } period && Enum.IsDefined(period) ? period : null });

    [HttpPost("/signup.html")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-registration")]
    public async Task<IActionResult> Signup(SignupViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = model.FullName.Trim(),
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                return EmailVerificationRequested(EmailLanguage());
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, "Member");
        if (model.MembershipPeriod is { } period && Enum.IsDefined(period))
            TempData["MembershipPeriod"] = (int)period;
        var language = EmailLanguage();
        cooldown.TryBegin("verification", userManager.NormalizeEmail(email)!);
        var delivery = await DeliverAccountEmailAsync(AccountEmailKind.Verify, user, language);
        return EmailVerificationRequested(language, delivery.DevelopmentPreviewUrl);
    }

    [HttpGet("/forgot-password.html")]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("/forgot-password.html")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        PrivateTokenResponse();
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var language = EmailLanguage();
        var user = cooldown.TryBegin("recovery", userManager.NormalizeEmail(email)!)
            ? await userManager.FindByEmailAsync(email) : null;

        // Always return the same message so this page cannot be used to discover accounts.
        model.Message = "If an eligible account exists and email delivery is available, a password reset link will be sent.";

        if (user is not null && user.EmailConfirmed && user.Status == AccountStatus.Active)
        {
            var delivery = await DeliverAccountEmailAsync(AccountEmailKind.Reset, user, language);
            model.DevelopmentResetUrl = delivery.DevelopmentPreviewUrl;
        }

        ModelState.Clear();
        return View(model);
    }

    [HttpGet("/reset-password.html")]
    [AllowAnonymous]
    public IActionResult ResetPassword(Guid userId, string? code)
    {
        PrivateTokenResponse();
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(code) || code.Length > 2048) return BadRequest();
        return View(new ResetPasswordViewModel { UserId = userId, Code = code });
    }

    [HttpPost("/reset-password.html")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        PrivateTokenResponse();
        if (!ModelState.IsValid || model.Code.Length > 2048) return View(model);

        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null || !user.EmailConfirmed || user.Status != AccountStatus.Active)
        {
            ModelState.AddModelError(string.Empty, "The password reset link is invalid or expired.");
            return View(model);
        }

        string decodedCode;
        try
        {
            decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "The password reset link is invalid or expired.");
            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, decodedCode, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["PasswordResetMessage"] = "Your password has been reset. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/verify-email.html")]
    [AllowAnonymous]
    public IActionResult VerifyEmail()
    {
        PrivateTokenResponse();
        return View(new VerifyEmailViewModel
        {
            DevelopmentConfirmationUrl = TempData["DevelopmentConfirmationUrl"] as string,
            Message = TempData["VerifyMessage"] as string
        });
    }

    [HttpGet("/account/confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string? code)
    {
        PrivateTokenResponse();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 2048)
        {
            TempData["VerifyMessage"] = "The confirmation link is invalid or expired.";
            return RedirectToAction(nameof(VerifyEmail), new { lang = EmailLanguage() });
        }
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            TempData["VerifyMessage"] = "The confirmation link is invalid or expired.";
            return RedirectToAction(nameof(VerifyEmail));
        }

        if (user.EmailConfirmed)
        {
            TempData["PasswordResetMessage"] = "Your email is already confirmed. You can sign in.";
            return RedirectToAction(nameof(Login));
        }

        string decoded;
        try { decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException)
        {
            TempData["VerifyMessage"] = "The confirmation link is invalid or expired.";
            return RedirectToAction(nameof(VerifyEmail));
        }

        if (user.Status != AccountStatus.Active || await userManager.IsLockedOutAsync(user))
        {
            TempData["VerifyMessage"] = "The confirmation link is invalid or expired.";
            return RedirectToAction(nameof(VerifyEmail), new { lang = EmailLanguage() });
        }

        var result = await userManager.ConfirmEmailAsync(user, decoded);
        if (!result.Succeeded)
        {
            TempData["VerifyMessage"] = "The confirmation link is invalid or expired.";
            return RedirectToAction(nameof(VerifyEmail));
        }

        if (user.Status != AccountStatus.Active)
        {
            await signInManager.SignOutAsync();
            TempData["PasswordResetMessage"] = "The email was confirmed, but this account cannot sign in.";
            return RedirectToAction(nameof(Login));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction(nameof(ProfileSetup), new { lang = EmailLanguage() });
    }

    [HttpPost("/account/resend-confirmation")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> ResendConfirmation(string? email)
    {
        var language = EmailLanguage();
        var address = email?.Trim();
        if (address is not { Length: > 0 and <= 254 } ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(address) ||
            !cooldown.TryBegin("verification", userManager.NormalizeEmail(address)!))
            return EmailVerificationRequested(language);

        var user = await userManager.FindByEmailAsync(address);
        var delivery = user is { EmailConfirmed: false, Status: AccountStatus.Active }
            ? await DeliverAccountEmailAsync(AccountEmailKind.Resend, user, language)
            : new AccountEmailDeliveryResult(false);
        return EmailVerificationRequested(language, delivery.DevelopmentPreviewUrl);
    }

    [HttpGet("/change-password.html")]
    [Authorize(Policy = SecurityPolicies.ActiveMember)]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost("/change-password.html"), ValidateAntiForgeryToken, EnableRateLimiting("account-sensitive")]
    [Authorize(Policy = SecurityPolicies.ActiveMember)]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.GetUserAsync(User);
        if (user is not { Status: AccountStatus.Active, EmailConfirmed: true } ||
            await userManager.IsLockedOutAsync(user)) return Forbid();
        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["ChangePasswordMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(ChangePassword), new { lang = AccountEmailLinks.Language(Request.Query["lang"].ToString()) });
    }

    [HttpGet("/welcome.html")]
    [Authorize(Policy = SecurityPolicies.CompleteProfile)]
    public async Task<IActionResult> Welcome()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!user.IsProfileComplete) return RedirectToAction(nameof(ProfileSetup));
        return View();
    }

    [HttpGet("/profile.html")]
    [Authorize(Policy = SecurityPolicies.CompleteProfile)]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!user.IsProfileComplete) return RedirectToAction(nameof(ProfileSetup));
        var skills = await db.UserSkills.AsNoTracking().Where(x => x.UserId == user.Id)
            .Select(x => new { x.Intent, Item = new UserSkillSummaryViewModel(x.SkillId, x.Skill.NameEn, x.Level) }).ToListAsync();
        var reviewsReceived = db.Reviews.AsNoTracking().ValidPeerReviews().Where(x => x.ReviewedUserId == user.Id);
        var reviewRows = await (from review in reviewsReceived
                             join reviewer in db.Users.AsNoTracking() on review.ReviewerId equals reviewer.Id
                             where review.ReviewedUserId == user.Id
                             orderby review.CreatedAtUtc descending
                             select new { reviewer.FullName, review.Rating, review.Comment, review.CreatedAtUtc })
            .Take(10).ToListAsync();
        var reviews = reviewRows.Select(x => new ReviewSummaryViewModel(x.FullName, NameFormatting.Initials(x.FullName), x.Rating, x.Comment, x.CreatedAtUtc)).ToList();
        var relatedIds = await db.LearningRequests.AsNoTracking()
            .Where(x => x.LearnerId == user.Id || x.TeacherId == user.Id)
            .Select(x => x.LearnerId == user.Id ? x.TeacherId : x.LearnerId).Distinct().ToListAsync();
        return View(new ProfileViewModel
        {
            FullName = user.FullName, Email = user.Email ?? string.Empty, Initials = NameFormatting.Initials(user.FullName),
            Location = user.Location, Bio = user.Bio,
            ActiveSessionsCount = await db.LearningSessions.CountAsync(x => x.Status == SessionStatus.Scheduled && (x.LearningRequest.LearnerId == user.Id || x.LearningRequest.TeacherId == user.Id)),
            LearnedSkillsCount = skills.Count(x => x.Intent == SkillIntent.Learn),
            ReviewsCount = await reviewsReceived.CountAsync(),
            ConnectionsCount = relatedIds.Count,
            AverageRating = await reviewsReceived.Select(x => (double?)x.Rating).AverageAsync(),
            TeachSkills = skills.Where(x => x.Intent == SkillIntent.Teach).Select(x => x.Item).ToList(),
            LearnSkills = skills.Where(x => x.Intent == SkillIntent.Learn).Select(x => x.Item).ToList(),
            Reviews = reviews
        });
    }

    [HttpGet("/profile-setup.html")]
    [Authorize(Policy = SecurityPolicies.ActiveMember)]
    public async Task<IActionResult> ProfileSetup()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new ProfileSetupViewModel
        {
            FullName = user.FullName,
            Location = user.Location,
            Bio = user.Bio,
            Skills = await GetSkillOptionsAsync()
        });
    }

    [HttpPost("/profile-setup.html")]
    [Authorize(Policy = SecurityPolicies.ActiveMember)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfileSetup(ProfileSetupViewModel model)
    {
        model.TeachSkillIds = model.TeachSkillIds.Distinct().ToList();
        model.LearnSkillIds = model.LearnSkillIds.Distinct().ToList();
        var validIds = await db.Skills.Where(x => x.Status == PublicationStatus.Published).Select(x => x.Id).ToListAsync();
        if (model.TeachSkillIds.Except(validIds).Any() || model.LearnSkillIds.Except(validIds).Any())
            ModelState.AddModelError(string.Empty, "One or more selected skills are invalid.");

        if (!ModelState.IsValid)
        {
            model.Skills = await GetSkillOptionsAsync();
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        user.FullName = model.FullName.Trim();
        user.Location = model.Location?.Trim();
        user.Bio = model.Bio?.Trim();
        user.IsProfileComplete = true;

        await db.UserSkills.Where(x => x.UserId == user.Id).ExecuteDeleteAsync();
        db.UserSkills.AddRange(model.LearnSkillIds.Select(id => new UserSkill { UserId = user.Id, SkillId = id, Intent = SkillIntent.Learn, Level = SkillLevel.Beginner }));
        db.UserSkills.AddRange(model.TeachSkillIds.Select(id => new UserSkill { UserId = user.Id, SkillId = id, Intent = SkillIntent.Teach, Level = SkillLevel.Intermediate }));
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync();
        if (TempData["MembershipPeriod"] is int selected && Enum.IsDefined((BillingPeriod)selected))
            return RedirectToAction("SubscriptionCheckout", "Membership", new { period = (BillingPeriod)selected });
        return Redirect("/welcome.html?mode=member");
    }

    [HttpPost("/account/logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/home.html");
    }

    [HttpGet("/dashboard.html")]
    [Authorize(Policy = SecurityPolicies.CompleteProfile)]
    public async Task<IActionResult> Dashboard()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!user.IsProfileComplete) return RedirectToAction(nameof(ProfileSetup));
        var skills = await db.UserSkills.AsNoTracking().Where(x => x.UserId == user.Id)
            .Select(x => new { x.Intent, Name = x.Skill.NameEn, x.SkillId }).ToListAsync();
        var recentRows = await db.LearningRequests.AsNoTracking()
            .Where(x => x.LearnerId == user.Id || x.TeacherId == user.Id)
            .OrderByDescending(x => x.CreatedAtUtc).Take(5)
            .Select(x => new { x.Id, PersonName = x.LearnerId == user.Id ? x.Teacher.FullName : x.Learner.FullName, SkillName = x.Skill.NameEn, x.Status }).ToListAsync();
        var recent = recentRows.Select(x => new DashboardRequestViewModel(x.Id, x.PersonName, NameFormatting.Initials(x.PersonName), x.SkillName, x.Status)).ToList();
        var validReviews = db.Reviews.AsNoTracking().ValidPeerReviews();
        var learnIds = skills.Where(x => x.Intent == SkillIntent.Learn).Select(x => x.SkillId).ToList();
        var partnerRows = await db.UserSkills.AsNoTracking()
            .Where(x => x.Intent == SkillIntent.Teach && learnIds.Contains(x.SkillId) && x.UserId != user.Id && x.User.Status == AccountStatus.Active)
            .OrderBy(x => x.User.FullName).Take(6)
            .Select(x => new { x.UserId, x.User.FullName, SkillName = x.Skill.NameEn,
                Rating = validReviews.Where(r => r.ReviewedUserId == x.UserId &&
                    r.LearningSession.LearningRequest.SkillId == x.SkillId).Select(r => (double?)r.Rating).Average() })
            .ToListAsync();
        var partners = partnerRows.Select(x => new PartnerSummaryViewModel(x.UserId, x.FullName, NameFormatting.Initials(x.FullName), x.SkillName, x.Rating)).ToList();
        var activeRelationships = db.LearningRequests.AsNoTracking()
            .Where(x => !x.IsExpertBooking && x.Status == RequestStatus.Accepted && x.EndedAtUtc == null &&
                (x.LearnerId == user.Id || x.TeacherId == user.Id));
        var connectionIds = await activeRelationships
            .Select(x => x.LearnerId == user.Id ? x.TeacherId : x.LearnerId).Distinct().ToListAsync();
        var now = clock.GetUtcNow();
        var plus = PlusEntitlement.Active(db, now.UtcDateTime).Where(s => s.UserId == user.Id);
        var credits = await db.Set<ExpertCreditAllocation>().AsNoTracking()
            .Where(c => c.StartsAtUtc <= now.UtcDateTime && now.UtcDateTime < c.EndsAtUtc &&
                plus.Any(s => s.Id == c.SubscriptionId))
            .Select(c => (int?)c.Remaining).FirstOrDefaultAsync();
        var peerSessions = db.LearningSessions.AsNoTracking().Where(s => !s.LearningRequest.IsExpertBooking &&
            s.LearningRequest.Status == RequestStatus.Accepted &&
            (s.LearningRequest.LearnerId == user.Id || s.LearningRequest.TeacherId == user.Id));
        var upcoming = peerSessions.Where(s => s.Status == SessionStatus.Scheduled && s.StartsAt > now);
        var completed = peerSessions.PeerCompletions();
        var myReceivedReviews = validReviews.Where(r => r.ReviewedUserId == user.Id);
        var awaitingScheduling = from pending in db.LearningSessionRequests.AsNoTracking()
            join relationship in db.LearningRequests on pending.LearningRequestId equals relationship.Id
            where pending.Status == LearningSessionRequestStatus.Pending && !relationship.IsExpertBooking &&
                  relationship.Status == RequestStatus.Accepted && relationship.EndedAtUtc == null &&
                  (relationship.TeacherId == user.Id || relationship.LearnerId == user.Id)
            orderby pending.CreatedAtUtc, pending.Id
            select new DashboardSchedulingRequestRow(relationship.Id, relationship.Skill.NameEn);
        return View(new DashboardViewModel
        {
            FullName = user.FullName, Email = user.Email ?? string.Empty, Initials = NameFormatting.Initials(user.FullName),
            RequestsCount = await db.LearningRequests.CountAsync(x => x.LearnerId == user.Id || x.TeacherId == user.Id),
            AvailableExpertCredits = credits,
            IncomingPendingPeerRequests = await db.LearningRequests.CountAsync(x =>
                x.TeacherId == user.Id && !x.IsExpertBooking && x.Status == RequestStatus.Pending),
            ActiveSessionsCount = await upcoming.CountAsync(),
            PendingLearnerSessionRequests = await awaitingScheduling.CountAsync(),
            ActiveLearningRelationships = await activeRelationships.CountAsync(),
            SchedulingRequests = await awaitingScheduling.Take(5).ToListAsync(),
            PendingAbsences = await db.SessionAbsenceRequests.CountAsync(p =>
                p.Status == SessionAbsenceRequestStatus.Pending && p.RequestedForStartsAtUtc > now &&
                upcoming.Any(s => s.Id == p.LearningSessionId)),
            CompletedTeachingSessions = await completed.CountAsync(s => s.LearningRequest.TeacherId == user.Id),
            CompletedLearningSessions = await completed.CountAsync(s => s.LearningRequest.LearnerId == user.Id),
            AwaitingMyReviews = await completed.CountAsync(s => s.LearningRequest.LearnerId == user.Id &&
                !db.Reviews.Any(r => r.LearningSessionId == s.Id)),
            ReceivedReviews = await myReceivedReviews.CountAsync(),
            TeacherAverageRating = await myReceivedReviews.Select(r => (double?)r.Rating).AverageAsync(),
            UpcomingSessions = await upcoming.OrderBy(s => s.StartsAt).ThenBy(s => s.Id).Take(5)
                .Select(s => new DashboardSessionRow(s.Id, s.Title ?? s.LearningRequest.Skill.NameEn, s.StartsAt)).ToListAsync(),
            LearnedSkillsCount = skills.Count(x => x.Intent == SkillIntent.Learn), ConnectionsCount = connectionIds.Count,
            TeachSkills = skills.Where(x => x.Intent == SkillIntent.Teach).Select(x => x.Name).ToList(),
            LearnSkills = skills.Where(x => x.Intent == SkillIntent.Learn).Select(x => x.Name).ToList(),
            RecentRequests = recent, RecommendedPartners = partners
        });
    }

    [HttpGet("/apply-expert.html")]
    [Authorize(Policy = SecurityPolicies.CompleteProfile)]
    public async Task<IActionResult> ApplyExpert()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!user.IsProfileComplete) return RedirectToAction(nameof(ProfileSetup));
        return View();
    }

    private string EmailLanguage() =>
        AccountEmailLinks.Language(Request.HasFormContentType ? Request.Form["EmailLanguage"].ToString() : Request.Query["lang"].ToString());

    private IActionResult EmailVerificationRequested(string language, string? preview = null)
    {
        TempData["DevelopmentConfirmationUrl"] = preview;
        TempData["VerifyMessage"] = "If your account is eligible, check your inbox and spam folder. If no message arrives, wait a minute and try again.";
        return RedirectToAction(nameof(VerifyEmail), new { lang = language });
    }

    private async Task<AccountEmailDeliveryResult> DeliverAccountEmailAsync(AccountEmailKind kind, ApplicationUser user, string language)
    {
        var reset = kind == AccountEmailKind.Reset;
        var token = reset ? await userManager.GeneratePasswordResetTokenAsync(user)
            : await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var url = emailLinks.Build(reset, user.Id, code, language);
        if (url is null || string.IsNullOrWhiteSpace(user.Email)) return new(false);
        return await emailSender.SendAsync(kind, user.Email, url, language, HttpContext.RequestAborted);
    }

    private void PrivateTokenResponse()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers.CacheControl = "no-store";
    }

    private async Task<IReadOnlyList<SkillOptionViewModel>> GetSkillOptionsAsync() =>
        await db.Skills.AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published)
            .OrderBy(x => x.SubCategory.Category.NameEn).ThenBy(x => x.NameEn)
            .Select(x => new SkillOptionViewModel(x.Id, x.NameEn, x.SubCategory.Category.NameEn, x.AccessTier))
            .ToListAsync();
}
