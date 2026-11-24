using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
namespace SkillsSwap.Services;

public sealed class AdminOperationsService(ApplicationDbContext db)
{
    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var adminRole=await db.Roles.Where(x=>x.Name=="Admin").Select(x=>(Guid?)x.Id).SingleOrDefaultAsync();
        var adminIds=adminRole.HasValue?db.UserRoles.Where(x=>x.RoleId==adminRole).Select(x=>x.UserId):db.UserRoles.Where(_=>false).Select(x=>x.UserId);
        return new AdminDashboardViewModel
        {
            Members=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id)), ActiveSkills=await db.Skills.CountAsync(x=>x.Status==PublicationStatus.Published),
            ApprovedExperts=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.Approved),
            ActiveSubscriptions=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.Active && x.EndsAtUtc>DateTime.UtcNow),
            PendingExpertApplications=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.UnderReview),
            ScheduledSessions=await db.LearningSessions.CountAsync(x=>x.Status==SessionStatus.Scheduled),
            PaidRevenue=await db.Payments.Where(x=>x.Status==PaymentStatus.Paid).SumAsync(x=>(decimal?)x.Amount) ?? 0,
            UnverifiedMembers=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id) && !x.EmailConfirmed),
            IncompleteProfiles=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id) && !x.IsProfileComplete),
            CareerPathsAwaitingReview=await db.CareerPaths.CountAsync(x=>x.Status==CareerPathStatus.UnderReview),
            SubscriptionsExpiringSoon=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.Active && x.EndsAtUtc>DateTime.UtcNow && x.EndsAtUtc<=DateTime.UtcNow.AddDays(7)),
            RecentMembers=await db.Users.AsNoTracking().Where(x=>!adminIds.Contains(x.Id)).OrderByDescending(x=>x.CreatedAtUtc).Take(5)
                .Select(x=>new AdminRecentMemberViewModel(x.Id,x.FullName,x.Email ?? string.Empty,x.CreatedAtUtc,x.EmailConfirmed)).ToListAsync(),
            RecentExpertApplications=await db.ExpertApplications.AsNoTracking().OrderByDescending(x=>x.SubmittedAtUtc).Take(5)
                .Select(x=>new AdminRecentExpertApplicationViewModel(x.Id,x.User.FullName,x.Skill.NameEn,x.Status,x.SubmittedAtUtc)).ToListAsync()
        };
    }
    public async Task<AdminPlansViewModel> GetPlansAsync()=>new(){Plans=await db.Plans.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new AdminPlanViewModel(x.Id,x.Code,x.Name,x.MonthlyPrice,x.YearlyPrice,x.MonthlyExpertCredits,x.IsActive,db.Subscriptions.Count(s=>s.PlanId==x.Id && s.Status==SubscriptionStatus.Active))).ToListAsync()};
    public async Task<bool> SavePlanAsync(AdminPlanFormViewModel model)
    {
        var plan=await db.Plans.FindAsync(model.Id); if(plan is null)return false;
        plan.Name=model.Name.Trim(); plan.MonthlyPrice=model.MonthlyPrice; plan.YearlyPrice=model.YearlyPrice; plan.MonthlyExpertCredits=model.MonthlyExpertCredits; plan.IsActive=model.IsActive;
        await db.SaveChangesAsync(); return true;
    }
    public async Task<AdminSubscriptionsViewModel> GetSubscriptionsAsync()=>new()
    {
        Active=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.Active), PastDue=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.PastDue), Cancelled=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.Cancelled),
        Subscriptions=await db.Subscriptions.AsNoTracking().OrderByDescending(x=>x.StartsAtUtc).Select(x=>new AdminSubscriptionRowViewModel(x.Id,x.User.FullName,x.User.Email ?? string.Empty,x.Plan.Name,x.Status,x.StartsAtUtc,x.EndsAtUtc,x.AutoRenew)).ToListAsync()
    };
    public async Task<bool> CancelSubscriptionAsync(int id)
    {
        var item=await db.Subscriptions.FindAsync(id); if(item is null)return false;
        if(item.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)return true;
        item.Status=SubscriptionStatus.Cancelled; item.AutoRenew=false; await db.SaveChangesAsync(); return true;
    }
    public async Task<AdminPaymentsViewModel> GetPaymentsAsync()=>new()
    {
        PaidTotal=await db.Payments.Where(x=>x.Status==PaymentStatus.Paid).SumAsync(x=>(decimal?)x.Amount)??0,
        PaidCount=await db.Payments.CountAsync(x=>x.Status==PaymentStatus.Paid), FailedCount=await db.Payments.CountAsync(x=>x.Status==PaymentStatus.Failed),
        Payments=await db.Payments.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new AdminPaymentRowViewModel(x.Id,x.Subscription.User.FullName,x.Subscription.User.Email ?? string.Empty,x.Subscription.Plan.Name,x.Provider,x.ProviderReference,x.Amount,x.Currency,x.Status,x.CreatedAtUtc)).ToListAsync()
    };
    public async Task<AdminReviewsViewModel> GetReviewsAsync()
    {
        var reviews=db.Reviews.AsNoTracking();
        return new AdminReviewsViewModel
        {
            Total=await reviews.CountAsync(),Positive=await reviews.CountAsync(x=>x.Rating>=4),Neutral=await reviews.CountAsync(x=>x.Rating==3),Critical=await reviews.CountAsync(x=>x.Rating<=2),
            Average=await reviews.AnyAsync()?await reviews.AverageAsync(x=>(double)x.Rating):null,
            Reviews=await reviews.OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new AdminReviewRowViewModel(x.Id,db.Users.Where(u=>u.Id==x.ReviewerId).Select(u=>u.FullName).FirstOrDefault() ?? "Member",
                db.Users.Where(u=>u.Id==x.ReviewedUserId).Select(u=>u.FullName).FirstOrDefault() ?? "Member",x.LearningSession.LearningRequest.Skill.NameEn,x.Rating,x.Comment,x.CreatedAtUtc)).ToListAsync()
        };
    }
    public async Task<AdminAnalyticsViewModel> GetAnalyticsAsync()
    {
        var reviews=db.Reviews.AsNoTracking();
        return new AdminAnalyticsViewModel
        {
            Members=await db.Users.CountAsync(),Sessions=await db.LearningSessions.CountAsync(),CompletedSessions=await db.LearningSessions.CountAsync(x=>x.Status==SessionStatus.Completed),
            Reviews=await reviews.CountAsync(),AverageRating=await reviews.AnyAsync()?await reviews.AverageAsync(x=>(double)x.Rating):null,CareerPaths=await db.CareerPaths.CountAsync(),
            ActivePlus=await db.Subscriptions.CountAsync(x=>x.Status==SubscriptionStatus.Active && x.Plan.Code=="PLUS"),Revenue=await db.Payments.Where(x=>x.Status==PaymentStatus.Paid).SumAsync(x=>(decimal?)x.Amount)??0,
            PopularSkills=await db.Skills.AsNoTracking().OrderByDescending(x=>db.LearningRequests.Count(r=>r.SkillId==x.Id)).Take(8)
                .Select(x=>new AdminSkillPopularityViewModel(x.NameEn,db.UserSkills.Count(u=>u.SkillId==x.Id&&u.Intent==SkillIntent.Learn),db.UserSkills.Count(u=>u.SkillId==x.Id&&u.Intent==SkillIntent.Teach),db.LearningRequests.Count(r=>r.SkillId==x.Id))).ToListAsync()
        };
    }
}
