using System.ComponentModel.DataAnnotations;
namespace SkillsSwap.Models;

public sealed class AdminDashboardViewModel
{
    public int Members { get; init; } public int ActiveSkills { get; init; } public int ApprovedExperts { get; init; }
    public int ActiveSubscriptions { get; init; } public int PendingExpertApplications { get; init; }
    public int ScheduledSessions { get; init; } public decimal PaidRevenue { get; init; }
    public int UnverifiedMembers { get; init; } public int IncompleteProfiles { get; init; }
    public int CareerPathsAwaitingReview { get; init; } public int SubscriptionsExpiringSoon { get; init; }
    public IReadOnlyList<AdminRecentMemberViewModel> RecentMembers { get; init; }=[];
    public IReadOnlyList<AdminRecentExpertApplicationViewModel> RecentExpertApplications { get; init; }=[];
}
public sealed record AdminRecentMemberViewModel(Guid Id,string Name,string Email,DateTime Joined,bool Verified);
public sealed record AdminRecentExpertApplicationViewModel(int Id,string Member,string Skill,ExpertApplicationStatus Status,DateTime Submitted);

public sealed class AdminPlansViewModel { public IReadOnlyList<AdminPlanViewModel> Plans { get; init; }=[]; }
public sealed record AdminPlanViewModel(int Id,string Code,string Name,decimal MonthlyPrice,decimal YearlyPrice,int MonthlyExpertCredits,bool IsActive,int Subscribers, decimal AdditionalExpertPrice, int ExpertSessionMinutes, decimal ExpertSharePercent);
public sealed class AdminPlanFormViewModel
{
    public int Id { get; set; }
    [Required,StringLength(80)] public string Name { get; set; }=string.Empty;
    [Range(0,100000)] public decimal MonthlyPrice { get; set; }
    [Range(0,100000)] public decimal YearlyPrice { get; set; }
    [Range(0,1000)] public int MonthlyExpertCredits { get; set; }
    public bool IsActive { get; set; }
    [Range(typeof(decimal), "0", "100000")] public decimal AdditionalExpertPrice { get; set; }
    [Range(0, 180)] public int ExpertSessionMinutes { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal ExpertSharePercent { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal PlatformSharePercent { get; set; }
}
public sealed class AdminSubscriptionsViewModel
{
    public int Active { get; init; } public int PastDue { get; init; } public int Cancelled { get; init; }
    public IReadOnlyList<AdminSubscriptionRowViewModel> Subscriptions { get; init; }=[];
}
public sealed record AdminSubscriptionRowViewModel(int Id,string UserName,string Email,string Plan,SubscriptionStatus Status,DateTime Starts,DateTime Ends,bool AutoRenew);
public sealed class AdminPaymentsViewModel
{
    public decimal PaidTotal { get; init; } public int PaidCount { get; init; } public int FailedCount { get; init; }
    public IReadOnlyList<AdminPaymentRowViewModel> Payments { get; init; }=[];
}
public sealed record AdminPaymentRowViewModel(long Id,string UserName,string Email,string Plan,string Provider,string Reference,decimal Amount,string Currency,PaymentStatus Status,DateTime CreatedAtUtc);
public sealed class AdminReviewsViewModel
{
    public int Total { get; init; } public int Positive { get; init; } public int Neutral { get; init; } public int Critical { get; init; }
    public double? Average { get; init; } public IReadOnlyList<AdminReviewRowViewModel> Reviews { get; init; }=[];
}
public sealed record AdminReviewRowViewModel(long Id,string Reviewer,string ReviewedUser,string Skill,byte Rating,string? Comment,DateTime CreatedAtUtc);
public sealed class AdminAnalyticsViewModel
{
    public int Members { get; init; } public int Sessions { get; init; } public int CompletedSessions { get; init; }
    public int Reviews { get; init; } public double? AverageRating { get; init; } public int CareerPaths { get; init; }
    public int ActivePlus { get; init; } public decimal Revenue { get; init; }
    public IReadOnlyList<AdminSkillPopularityViewModel> PopularSkills { get; init; }=[];
}
public sealed record AdminSkillPopularityViewModel(string Skill,int Learners,int Teachers,int Requests);
