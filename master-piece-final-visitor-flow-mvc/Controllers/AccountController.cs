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
    IAccountEmailSender emailSender) : Controller
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
    public IActionResult Signup() => View(new SignupViewModel());

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
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, "Member");
        var url = await BuildConfirmationUrlAsync(user);
        var delivery = await emailSender.SendConfirmationAsync(email, url, HttpContext.RequestAborted);
        TempData["ConfirmationEmail"] = email;
        TempData["DevelopmentConfirmationUrl"] = delivery.DevelopmentPreviewUrl;
        TempData["VerifyMessage"] = delivery.Accepted
            ? "A confirmation email has been sent."
            : delivery.DevelopmentPreviewUrl is not null
                ? "Development email preview is enabled."
                : "We could not send the confirmation email. Please try resending it later.";
        return RedirectToAction(nameof(VerifyEmail));
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
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        // Always return the same message so this page cannot be used to discover accounts.
        model.Message = "If an eligible account exists and email delivery is available, a password reset link will be sent.";

        if (user is not null && user.EmailConfirmed && user.Status == AccountStatus.Active)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Action(nameof(ResetPassword), "Account",
                new { userId = user.Id, code }, Request.Scheme)!;

            var delivery = await emailSender.SendPasswordResetAsync(email, resetUrl, HttpContext.RequestAborted);
            model.DevelopmentResetUrl = delivery.DevelopmentPreviewUrl;
        }

        ModelState.Clear();
        return View(model);
    }

    [HttpGet("/reset-password.html")]
    [AllowAnonymous]
    public IActionResult ResetPassword(Guid userId, string? code)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(code)) return BadRequest();
        return View(new ResetPasswordViewModel { UserId = userId, Code = code });
    }

    [HttpPost("/reset-password.html")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
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
    public IActionResult VerifyEmail() => View(new VerifyEmailViewModel
    {
        Email = TempData.Peek("ConfirmationEmail") as string ?? string.Empty,
        DevelopmentConfirmationUrl = TempData.Peek("DevelopmentConfirmationUrl") as string,
        Message = TempData["VerifyMessage"] as string
    });

    [HttpGet("/account/confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string code)
    {
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
        return Redirect("/profile-setup.html");
    }

    [HttpPost("/account/resend-confirmation")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-sensitive")]
    public async Task<IActionResult> ResendConfirmation(string? email)
    {
        var normalizedEmail = email?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var user = await userManager.FindByEmailAsync(normalizedEmail);
            if (user is not null && !user.EmailConfirmed)
            {
                var url = await BuildConfirmationUrlAsync(user);
                var delivery = await emailSender.SendConfirmationAsync(normalizedEmail, url, HttpContext.RequestAborted);
                TempData["ConfirmationEmail"] = normalizedEmail;
                TempData["DevelopmentConfirmationUrl"] = delivery.DevelopmentPreviewUrl;
            }
        }
        TempData["VerifyMessage"] = "If an eligible unconfirmed account exists and delivery is available, a new confirmation email will be sent.";
        return RedirectToAction(nameof(VerifyEmail));
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
        var now = DateTimeOffset.UtcNow;
        var peerSessions = db.LearningSessions.AsNoTracking().Where(s => !s.LearningRequest.IsExpertBooking &&
            s.LearningRequest.Status == RequestStatus.Accepted &&
            (s.LearningRequest.LearnerId == user.Id || s.LearningRequest.TeacherId == user.Id));
        var upcoming = peerSessions.Where(s => s.Status == SessionStatus.Scheduled && s.StartsAt > now);
        var elapsed = peerSessions.Where(s => s.Status == SessionStatus.Scheduled &&
            s.LearningRequest.EndedAtUtc == null && s.StartsAt.AddMinutes(s.DurationMinutes) <= now);
        var completed = peerSessions.VerifiedPeerCompletions();
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
            IncomingPendingPeerRequests = await db.LearningRequests.CountAsync(x =>
                x.TeacherId == user.Id && !x.IsExpertBooking && x.Status == RequestStatus.Pending),
            ActiveSessionsCount = await upcoming.CountAsync(),
            PendingLearnerSessionRequests = await awaitingScheduling.CountAsync(),
            ActiveLearningRelationships = await activeRelationships.CountAsync(),
            SchedulingRequests = await awaitingScheduling.Take(5).ToListAsync(),
            PendingAbsences = await db.SessionAbsenceRequests.CountAsync(p =>
                p.Status == SessionAbsenceRequestStatus.Pending && p.RequestedForStartsAtUtc > now &&
                upcoming.Any(s => s.Id == p.LearningSessionId)),
            AwaitingMyCompletion = await elapsed.CountAsync(s => s.LearningRequest.TeacherId == user.Id
                ? s.TeacherCompletedAtUtc == null : s.LearnerCompletedAtUtc == null),
            AwaitingOtherCompletion = await elapsed.CountAsync(s => s.LearningRequest.TeacherId == user.Id
                ? s.TeacherCompletedAtUtc != null : s.LearnerCompletedAtUtc != null),
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

    private async Task<string> BuildConfirmationUrlAsync(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, Request.Scheme)!;
    }

    private async Task<IReadOnlyList<SkillOptionViewModel>> GetSkillOptionsAsync() =>
        await db.Skills.AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published)
            .OrderBy(x => x.SubCategory.Category.NameEn).ThenBy(x => x.NameEn)
            .Select(x => new SkillOptionViewModel(x.Id, x.NameEn, x.SubCategory.Category.NameEn, x.AccessTier))
            .ToListAsync();
}
