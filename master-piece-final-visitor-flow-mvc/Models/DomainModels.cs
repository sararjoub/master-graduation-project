using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public enum AccountStatus { Active, Suspended, Closed }
public enum SkillIntent { Learn, Teach }
public enum SkillLevel { Beginner, Intermediate, Advanced }
public enum AccessTier { Free, Plus }
public enum PublicationStatus { Draft, Published, Archived }
public enum RequestStatus { Pending, Accepted, Declined, Cancelled, Completed }
public enum ExpertApplicationStatus { UnderReview, NeedsInformation, Approved, Rejected, Withdrawn }
public enum SubscriptionStatus { Pending, Active, PastDue, Cancelled, Expired }
public enum PaymentStatus { Pending, Paid, Failed, Refunded }
public enum SessionStatus { Scheduled, Completed, Cancelled, NoShow }
public enum CareerPathStatus { Draft, UnderReview, ChangesRequested, Approved, Active, Completed, Archived }
public enum ProgressStatus { Locked, Available, InProgress, Completed }

public sealed class Category
{
    public int Id { get; set; }
    [MaxLength(80)] public string NameAr { get; set; } = string.Empty;
    [MaxLength(80)] public string NameEn { get; set; } = string.Empty;
    [MaxLength(80)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SubCategory> SubCategories { get; set; } = [];
}

public sealed class SubCategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    [MaxLength(80)] public string NameAr { get; set; } = string.Empty;
    [MaxLength(80)] public string NameEn { get; set; } = string.Empty;
    [MaxLength(80)] public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Skill
{
    public int Id { get; set; }
    public int SubCategoryId { get; set; }
    public SubCategory SubCategory { get; set; } = null!;
    [MaxLength(120)] public string NameAr { get; set; } = string.Empty;
    [MaxLength(120)] public string NameEn { get; set; } = string.Empty;
    [MaxLength(120)] public string Slug { get; set; } = string.Empty;
    [MaxLength(1500)] public string Description { get; set; } = string.Empty;
    public SkillLevel Level { get; set; }
    public AccessTier AccessTier { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
}

public sealed class UserSkill
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public SkillIntent Intent { get; set; }
    public SkillLevel Level { get; set; }
    [MaxLength(1000)] public string? Experience { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ExpertApplication
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    [MaxLength(3000)] public string Experience { get; set; } = string.Empty;
    [MaxLength(2000)] public string TeachingApproach { get; set; } = string.Empty;
    [MaxLength(500)] public string? PortfolioUrl { get; set; }
    [MaxLength(500)] public string? SampleUrl { get; set; }
    public ExpertApplicationStatus Status { get; set; } = ExpertApplicationStatus.UnderReview;
    [MaxLength(1500)] public string? AdminReason { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class ExpertAvailability
{
    public int Id { get; set; }
    public int ExpertApplicationId { get; set; }
    public ExpertApplication ExpertApplication { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    [MaxLength(60)] public string TimeZoneId { get; set; } = "Asia/Amman";
}

public sealed class Plan
{
    public int Id { get; set; }
    [MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(80)] public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MonthlyExpertCredits { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Subscription
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool AutoRenew { get; set; }
}

public sealed class Payment
{
    public long Id { get; set; }
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    [MaxLength(100)] public string Provider { get; set; } = string.Empty;
    [MaxLength(200)] public string ProviderReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// A learner asks one selected teacher for help. No reciprocal skill is required.
public sealed class LearningRequest
{
    public long Id { get; set; }
    public Guid LearnerId { get; set; }
    public ApplicationUser Learner { get; set; } = null!;
    public Guid TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public bool IsExpertBooking { get; set; }
    [MaxLength(1500)] public string Message { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
}

public sealed class LearningSession
{
    public long Id { get; set; }
    public long LearningRequestId { get; set; }
    public LearningRequest LearningRequest { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    [MaxLength(500)] public string? MeetingJoinUrl { get; set; }
    [MaxLength(200)] public string? MeetingProviderId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
}

public sealed class Message
{
    public long Id { get; set; }
    public long LearningRequestId { get; set; }
    public LearningRequest LearningRequest { get; set; } = null!;
    public Guid SenderId { get; set; }
    public ApplicationUser Sender { get; set; } = null!;
    [MaxLength(4000)] public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class Review
{
    public long Id { get; set; }
    public long LearningSessionId { get; set; }
    public LearningSession LearningSession { get; set; } = null!;
    public Guid ReviewerId { get; set; }
    public Guid ReviewedUserId { get; set; }
    public byte Rating { get; set; }
    [MaxLength(1500)] public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CareerPath
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    [MaxLength(200)] public string Goal { get; set; } = string.Empty;
    [MaxLength(30)] public string CurrentLevel { get; set; } = string.Empty;
    public int WeeklyHours { get; set; }
    public bool GeneratedByAi { get; set; }
    public CareerPathStatus Status { get; set; } = CareerPathStatus.Draft;
    [MaxLength(1500)] public string? AdminReviewNote { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public ICollection<CareerPathStep> Steps { get; set; } = [];
}

public sealed class CareerPathStep
{
    public long Id { get; set; }
    public long CareerPathId { get; set; }
    public CareerPath CareerPath { get; set; } = null!;
    public int Order { get; set; }
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string LearningOutcome { get; set; } = string.Empty;
    public AccessTier RequiredTier { get; set; }
    public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.Locked;
    public DateTime? CompletedAtUtc { get; set; }
}
