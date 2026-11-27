namespace SkillsSwap.Models;

public sealed class CategoriesPageViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? CurrentUserFullName { get; set; }
    public string? CurrentUserEmail { get; set; }
    public string CurrentUserInitials { get; set; } = string.Empty;
    public IReadOnlyList<CategoryCardViewModel> Categories { get; set; } = [];
}

public sealed record CategoryCardViewModel(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int SubCategoryCount,
    int SkillCount);

public sealed class CategoryOverviewViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<SubCategoryCardViewModel> SubCategories { get; set; } = [];
}

public sealed record SubCategoryCardViewModel(int Id, string Name, string Slug, int SkillCount);

public sealed class SubCategoryPageViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public IReadOnlyList<SkillCardViewModel> Skills { get; set; } = [];
}

public sealed record SkillCardViewModel(
    int Id,
    string Name,
    string Slug,
    string Description,
    SkillLevel Level,
    AccessTier AccessTier);

public sealed class SkillDetailsViewModel
{
    public bool HasPlus { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SkillLevel Level { get; set; }
    public AccessTier AccessTier { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string SubCategoryName { get; set; } = string.Empty;
    public string SubCategorySlug { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public IReadOnlyList<RelatedSkillViewModel> RelatedSkills { get; set; } = [];
}

public sealed record RelatedSkillViewModel(string Name, string Slug);

public enum DiscoverySort { Newest, Name, Rating }

public sealed class SkillDiscoveryQuery
{
    public string? Search { get; set; }
    public SkillLevel? Level { get; set; }
    public DiscoverySort Sort { get; set; } = DiscoverySort.Newest;
    public int Page { get; set; } = 1;
}

public sealed class SkillDiscoveryViewModel
{
    public AccessTier AccessTier { get; init; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string SkillSlug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string SubCategoryName { get; set; } = string.Empty;
    public string SubCategorySlug { get; set; } = string.Empty;
    public string? Search { get; set; }
    public SkillLevel? Level { get; set; }
    public DiscoverySort Sort { get; set; }
    public IReadOnlyList<DiscoveryPersonViewModel> Partners { get; set; } = [];
    public IReadOnlyList<DiscoveryPersonViewModel> Experts { get; set; } = [];
    public IReadOnlyList<RelatedSkillViewModel> RelatedSkills { get; set; } = [];
    public int PartnerCount { get; set; }
    public int ExpertCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || Level.HasValue;
}

public sealed class DiscoveryPersonViewModel
{
    public long? PendingPeerRequestId { get; set; }
    public long? AcceptedRelationshipId { get; set; }
    public int SkillId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Biography { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public SkillLevel TeachingLevel { get; set; }
    public string? Experience { get; set; }
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedTeachingSessions { get; set; }
    public bool IsVerifiedExpert { get; set; }
    public DateTime AddedAtUtc { get; set; }
}

public sealed class DashboardViewModel
{
    public int? AvailableExpertCredits { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int RequestsCount { get; set; }
    public int IncomingPendingPeerRequests { get; set; }
    public int PendingLearnerSessionRequests { get; set; }
    public int ActiveLearningRelationships { get; set; }
    public IReadOnlyList<DashboardSchedulingRequestRow> SchedulingRequests { get; set; } = [];
    public int PendingAbsences { get; set; }
    public int CompletedTeachingSessions { get; set; }
    public int CompletedLearningSessions { get; set; }
    public int AwaitingMyReviews { get; set; }
    public int ReceivedReviews { get; set; }
    public double? TeacherAverageRating { get; set; }
    public IReadOnlyList<DashboardSessionRow> UpcomingSessions { get; set; } = [];
    public int ActiveSessionsCount { get; set; }
    public int LearnedSkillsCount { get; set; }
    public int ConnectionsCount { get; set; }
    public IReadOnlyList<string> TeachSkills { get; set; } = [];
    public IReadOnlyList<string> LearnSkills { get; set; } = [];
    public IReadOnlyList<DashboardRequestViewModel> RecentRequests { get; set; } = [];
    public IReadOnlyList<PartnerSummaryViewModel> RecommendedPartners { get; set; } = [];
}

public sealed record DashboardRequestViewModel(long Id, string PersonName, string PersonInitials, string SkillName, RequestStatus Status);
public sealed record PartnerSummaryViewModel(Guid UserId, string FullName, string Initials, string SkillName, double? Rating);

public sealed class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Bio { get; set; }
    public int ActiveSessionsCount { get; set; }
    public int LearnedSkillsCount { get; set; }
    public int ReviewsCount { get; set; }
    public int ConnectionsCount { get; set; }
    public double? AverageRating { get; set; }
    public IReadOnlyList<UserSkillSummaryViewModel> TeachSkills { get; set; } = [];
    public IReadOnlyList<UserSkillSummaryViewModel> LearnSkills { get; set; } = [];
    public IReadOnlyList<ReviewSummaryViewModel> Reviews { get; set; } = [];
}

public sealed record UserSkillSummaryViewModel(int SkillId, string Name, SkillLevel Level);
public sealed record ReviewSummaryViewModel(string ReviewerName, string ReviewerInitials, byte Rating, string? Comment, DateTime CreatedAtUtc);

public static class NameFormatting
{
    public static string Initials(string? fullName)
    {
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}
