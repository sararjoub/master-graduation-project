using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class AdminPeopleService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
{
    private const int PageSize = 8;

    public async Task<AdminUsersViewModel> GetUsersAsync(string? search, string? filter, int page)
    {
        search = search?.Trim() ?? string.Empty;
        filter = (filter ?? "all").ToLowerInvariant();
        var adminRoleId = await db.Roles.Where(x => x.Name == "Admin").Select(x => (Guid?)x.Id).SingleOrDefaultAsync();
        var adminIds = adminRoleId.HasValue ? db.UserRoles.Where(x => x.RoleId == adminRoleId).Select(x => x.UserId) : db.UserRoles.Where(_ => false).Select(x => x.UserId);
        var members = db.Users.AsNoTracking().Where(x => !adminIds.Contains(x.Id));
        var approvedExperts = db.ExpertApplications.Where(x => x.Status == ExpertApplicationStatus.Approved).Select(x => x.UserId);

        var total = await members.CountAsync();
        var verified = await members.CountAsync(x => x.EmailConfirmed);
        var suspended = await members.CountAsync(x => x.Status == AccountStatus.Suspended);
        var experts = await members.CountAsync(x => approvedExperts.Contains(x.Id));

        if (search.Length > 0) members = members.Where(x => x.FullName.Contains(search) || (x.Email != null && x.Email.Contains(search)));
        members = filter switch
        {
            "active" => members.Where(x => x.Status == AccountStatus.Active),
            "suspended" => members.Where(x => x.Status == AccountStatus.Suspended),
            "unverified" => members.Where(x => !x.EmailConfirmed),
            "experts" => members.Where(x => approvedExperts.Contains(x.Id)),
            _ => members
        };

        var count = await members.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        var rows = await members.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new AdminUserRowViewModel(x.Id, x.FullName, x.Email ?? string.Empty, x.EmailConfirmed,
                x.Status, x.IsProfileComplete, approvedExperts.Contains(x.Id), x.CreatedAtUtc)).ToListAsync();

        return new AdminUsersViewModel { Search=search, Filter=filter, Page=page, PageCount=pages,
            TotalUsers=total, VerifiedUsers=verified, SuspendedUsers=suspended, ExpertUsers=experts, Users=rows };
    }

    public async Task<AdminUserDetailsViewModel?> GetUserAsync(Guid id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user is null || await userManager.IsInRoleAsync(user, "Admin")) return null;
        var skills = await db.UserSkills.AsNoTracking().Where(x => x.UserId == id)
            .Select(x => new { x.Intent, x.Skill.NameEn }).ToListAsync();
        var ratings = db.Reviews.AsNoTracking().Where(x => x.ReviewedUserId == id);
        return new AdminUserDetailsViewModel
        {
            Id=user.Id, FullName=user.FullName, Email=user.Email ?? string.Empty, Location=user.Location, Bio=user.Bio,
            EmailConfirmed=user.EmailConfirmed, IsProfileComplete=user.IsProfileComplete, Status=user.Status, CreatedAtUtc=user.CreatedAtUtc,
            CompletedSessions=await db.LearningSessions.CountAsync(x => x.Status==SessionStatus.Completed && (x.LearningRequest.LearnerId==id || x.LearningRequest.TeacherId==id)),
            PendingRequests=await db.LearningRequests.CountAsync(x => x.Status==RequestStatus.Pending && (x.LearnerId==id || x.TeacherId==id)),
            Reviews=await ratings.CountAsync(), Rating=await ratings.AnyAsync() ? await ratings.AverageAsync(x => (double)x.Rating) : null,
            TeachSkills=skills.Where(x=>x.Intent==SkillIntent.Teach).Select(x=>x.NameEn).ToList(),
            LearnSkills=skills.Where(x=>x.Intent==SkillIntent.Learn).Select(x=>x.NameEn).ToList(),
            ExpertSkills=await db.ExpertApplications.AsNoTracking().Where(x=>x.UserId==id && x.Status==ExpertApplicationStatus.Approved).Select(x=>x.Skill.NameEn).ToListAsync()
        };
    }

    public async Task<AdminUserStatusFormViewModel?> GetStatusFormAsync(Guid id)
    {
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        if(user is null || await userManager.IsInRoleAsync(user,"Admin")) return null;
        return new AdminUserStatusFormViewModel { Id=user.Id, FullName=user.FullName, Email=user.Email ?? string.Empty, Status=user.Status };
    }

    public async Task<bool> ChangeStatusAsync(AdminUserStatusFormViewModel model)
    {
        var user=await userManager.FindByIdAsync(model.Id.ToString());
        if(user is null || await userManager.IsInRoleAsync(user,"Admin")) return false;
        user.Status=model.Status;
        var result=await userManager.UpdateAsync(user);
        if(result.Succeeded) await userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded;
    }

    public async Task<AdminExpertApplicationsViewModel> GetExpertApplicationsAsync(string? status)
    {
        status=(status ?? "all").ToLowerInvariant();
        var query=db.ExpertApplications.AsNoTracking();
        if(Enum.TryParse<ExpertApplicationStatus>(status,true,out var parsed)) query=query.Where(x=>x.Status==parsed);
        return new AdminExpertApplicationsViewModel
        {
            Status=status,
            UnderReviewCount=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.UnderReview),
            ApprovedCount=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.Approved),
            NeedsInformationCount=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.NeedsInformation),
            RejectedCount=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.Rejected),
            Applications=await query.OrderByDescending(x=>x.SubmittedAtUtc).Select(x=>new AdminExpertApplicationRowViewModel(
                x.Id,x.UserId,x.User.FullName,x.User.Email ?? string.Empty,x.Skill.NameEn,x.Experience,x.TeachingApproach,
                x.PortfolioUrl,x.SampleUrl,x.Status,x.AdminReason,x.SubmittedAtUtc,x.ReviewedAtUtc)).ToListAsync()
        };
    }

    public async Task<string> DecideExpertApplicationAsync(ExpertDecisionViewModel model, Guid adminId)
    {
        if(model.Decision is not (ExpertApplicationStatus.Approved or ExpertApplicationStatus.NeedsInformation or ExpertApplicationStatus.Rejected)) return "InvalidDecision";
        var application=await db.ExpertApplications.SingleOrDefaultAsync(x=>x.Id==model.Id);
        if(application is null) return "NotFound";
        application.Status=model.Decision; application.AdminReason=model.Reason.Trim();
        application.ReviewedByUserId=adminId; application.ReviewedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(); return "Success";
    }
}
