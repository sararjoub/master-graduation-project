using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
namespace SkillsSwap.Services;

public sealed class AdminOperationsService(ApplicationDbContext db, TimeProvider clock, PeerSessionService locks)
{
    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var adminRole=await db.Roles.Where(x=>x.Name=="Admin").Select(x=>(Guid?)x.Id).SingleOrDefaultAsync();
        var adminIds=adminRole.HasValue?db.UserRoles.Where(x=>x.RoleId==adminRole).Select(x=>x.UserId):db.UserRoles.Where(_=>false).Select(x=>x.UserId);
        return new AdminDashboardViewModel
        {
            Members=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id)), ActiveSkills=await db.Skills.CountAsync(x=>x.Status==PublicationStatus.Published),
            ApprovedExperts=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.Approved),
            ActiveSubscriptions=await PlusEntitlement.Active(db, clock.GetUtcNow().UtcDateTime).CountAsync(),
            PendingExpertApplications=await db.ExpertApplications.CountAsync(x=>x.Status==ExpertApplicationStatus.UnderReview),
            ScheduledSessions=await db.LearningSessions.CountAsync(x=>x.Status==SessionStatus.Scheduled),
            PaidRevenue=await db.Payments.Where(x=>x.Status==PaymentStatus.Paid && x.Provider==SimulatedPaymentGateway.ProviderName).SumAsync(x=>(decimal?)x.Amount) ?? 0,
            UnverifiedMembers=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id) && !x.EmailConfirmed),
            IncompleteProfiles=await db.Users.CountAsync(x=>!adminIds.Contains(x.Id) && !x.IsProfileComplete),
            CareerPathsAwaitingReview=await db.CareerPaths.CountAsync(x=>x.Status==CareerPathStatus.UnderReview),
            SubscriptionsExpiringSoon=await PlusEntitlement.Active(db, clock.GetUtcNow().UtcDateTime).CountAsync(x=>x.EndsAtUtc<=clock.GetUtcNow().UtcDateTime.AddDays(7)),
            RecentMembers=await db.Users.AsNoTracking().Where(x=>!adminIds.Contains(x.Id)).OrderByDescending(x=>x.CreatedAtUtc).Take(5)
                .Select(x=>new AdminRecentMemberViewModel(x.Id,x.FullName,x.Email ?? string.Empty,x.CreatedAtUtc,x.EmailConfirmed)).ToListAsync(),
            RecentExpertApplications=await db.ExpertApplications.AsNoTracking().OrderByDescending(x=>x.SubmittedAtUtc).Take(5)
                .Select(x=>new AdminRecentExpertApplicationViewModel(x.Id,x.User.FullName,x.Skill.NameEn,x.Status,x.SubmittedAtUtc)).ToListAsync()
        };
    }
    public async Task<AdminPlansViewModel> GetPlansAsync()
    {
        var active = PlusEntitlement.Active(db, clock.GetUtcNow().UtcDateTime);
        return new() { Plans = await db.Plans.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new AdminPlanViewModel(p.Id, p.Code, p.Name, p.MonthlyPrice, p.YearlyPrice,
                p.MonthlyExpertCredits, p.IsActive, active.Count(s => s.PlanId == p.Id),
                p.AdditionalExpertPrice, p.ExpertSessionMinutes, p.ExpertSharePercent)).ToListAsync() };
    }
    public async Task<bool> SavePlanAsync(AdminPlanFormViewModel model)
    {
        var plan=await db.Plans.FindAsync(model.Id); if(plan is null)return false;
        if (plan.Code != SubscriptionTerms.Plus || model.MonthlyPrice <= 0 || model.YearlyPrice <= 0 ||
            model.AdditionalExpertPrice <= 0 || model.ExpertSessionMinutes < 1 ||
            model.MonthlyExpertCredits is < 0 or > 1000 || model.ExpertSessionMinutes > 180 ||
            model.ExpertSharePercent is < 0 or > 100 || model.PlatformSharePercent is < 0 or > 100 ||
            model.ExpertSharePercent + model.PlatformSharePercent != 100 ||
            new[] { model.MonthlyPrice, model.YearlyPrice, model.AdditionalExpertPrice, model.ExpertSharePercent, model.PlatformSharePercent }
                .Any(value => decimal.Round(value, 2) != value))
            return false;
        plan.MonthlyPrice = model.MonthlyPrice;
        plan.YearlyPrice = model.YearlyPrice;
        plan.MonthlyExpertCredits = model.MonthlyExpertCredits;
        plan.AdditionalExpertPrice = model.AdditionalExpertPrice;
        plan.ExpertSessionMinutes = model.ExpertSessionMinutes;
        plan.ExpertSharePercent = model.ExpertSharePercent;
        plan.IsActive = model.IsActive;
        await db.SaveChangesAsync(); return true;
    }
    public async Task<AdminSubscriptionsViewModel> GetSubscriptionsAsync(string? search, SubscriptionStatus? status, int? id)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var active = PlusEntitlement.Active(db, now);
        var rows = db.Subscriptions.AsNoTracking().Where(s => id == null || s.Id == id);
        if (!string.IsNullOrWhiteSpace(search))
            rows = rows.Where(s => s.User.FullName.Contains(search) || (s.User.Email ?? "").Contains(search));
        var projected = rows.Select(s => new
        {
            s.Id, s.User.FullName, s.User.Email, s.Plan.Name, s.StartsAtUtc, s.EndsAtUtc, s.AutoRenew,
            Effective = s.Status == SubscriptionStatus.Active && s.EndsAtUtc <= now ? SubscriptionStatus.Expired :
                s.Status == SubscriptionStatus.Active && (s.StartsAtUtc > now || !active.Any(a => a.Id == s.Id))
                    ? SubscriptionStatus.Pending : s.Status
        });
        if (status.HasValue) projected = projected.Where(s => s.Effective == status.Value);
        return new()
        {
            Active = await active.CountAsync(),
            PastDue = await db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.PastDue),
            Cancelled = await db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Cancelled),
            Subscriptions = await projected.OrderByDescending(s => s.Id).Take(100)
                .Select(s => new AdminSubscriptionRowViewModel(s.Id, s.FullName, s.Email ?? "", s.Name,
                    s.Effective, s.StartsAtUtc, s.EndsAtUtc, s.AutoRenew)).ToListAsync()
        };
    }
    public async Task<bool> CancelSubscriptionAsync(int id)
    {
        var user = await db.Subscriptions.Where(s => s.Id == id).Select(s => (Guid?)s.UserId).SingleOrDefaultAsync();
        if (user is null) return false;
        await using var tx = await db.Database.BeginTransactionAsync();
        if (!await locks.LockAsync($"SkillSwap:Subscription:{user:D}", default)) return false;
        var item=await db.Subscriptions.FindAsync(id); if(item is null)return false;
        if(item.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)return true;
        if (item.Status == SubscriptionStatus.Pending) return false;
        item.Status=SubscriptionStatus.Cancelled; item.AutoRenew=false; await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }
    public async Task<AdminPaymentsViewModel> GetPaymentsAsync(string? search, PaymentStatus? status, long? id)
    {
        var rows = db.Payments.AsNoTracking().Where(p => id == null || p.Id == id);
        if (!string.IsNullOrWhiteSpace(search))
            rows = rows.Where(p => p.ProviderReference.Contains(search) || p.Subscription.User.FullName.Contains(search)
                || (p.Subscription.User.Email ?? "").Contains(search));
        if (status.HasValue) rows = rows.Where(p => p.Status == status.Value);
        return new()
        {
            PaidTotal = await db.Payments.Where(x => x.Status == PaymentStatus.Paid && x.Provider == SimulatedPaymentGateway.ProviderName)
                .SumAsync(x => (decimal?)x.Amount) ?? 0,
            PaidCount = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Paid && x.Provider == SimulatedPaymentGateway.ProviderName),
            FailedCount = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Failed && x.Provider == SimulatedPaymentGateway.ProviderName),
            Payments = await rows.OrderByDescending(x => x.Id).Take(100).Select(x => new AdminPaymentRowViewModel(
                x.Id,x.Subscription.User.FullName,x.Subscription.User.Email ?? "",x.Subscription.Plan.Name,x.Provider,
                x.ProviderReference,x.Amount,x.Currency,x.Status,x.CreatedAtUtc)).ToListAsync()
        };
    }
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
            ActivePlus=await PlusEntitlement.Active(db, clock.GetUtcNow().UtcDateTime).CountAsync(),Revenue=await db.Payments.Where(x=>x.Status==PaymentStatus.Paid && x.Provider==SimulatedPaymentGateway.ProviderName).SumAsync(x=>(decimal?)x.Amount)??0,
            PopularSkills=await db.Skills.AsNoTracking().OrderByDescending(x=>db.LearningRequests.Count(r=>r.SkillId==x.Id)).Take(8)
                .Select(x=>new AdminSkillPopularityViewModel(x.NameEn,db.UserSkills.Count(u=>u.SkillId==x.Id&&u.Intent==SkillIntent.Learn),db.UserSkills.Count(u=>u.SkillId==x.Id&&u.Intent==SkillIntent.Teach),db.LearningRequests.Count(r=>r.SkillId==x.Id))).ToListAsync()
        };
    }
}
