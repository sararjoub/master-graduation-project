using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public sealed class AdminUsersViewModel
{
    public string Search { get; init; } = string.Empty;
    public string Filter { get; init; } = "all";
    public int Page { get; init; }
    public int PageCount { get; init; }
    public int TotalUsers { get; init; }
    public int VerifiedUsers { get; init; }
    public int SuspendedUsers { get; init; }
    public int ExpertUsers { get; init; }
    public IReadOnlyList<AdminUserRowViewModel> Users { get; init; } = [];
}

public sealed record AdminUserRowViewModel(Guid Id, string FullName, string Email, bool EmailConfirmed,
    AccountStatus Status, bool IsProfileComplete, bool IsExpert, DateTime CreatedAtUtc);

public sealed class AdminUserDetailsViewModel
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Bio { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsProfileComplete { get; init; }
    public AccountStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int CompletedSessions { get; init; }
    public int PendingRequests { get; init; }
    public int Reviews { get; init; }
    public double? Rating { get; init; }
    public IReadOnlyList<string> TeachSkills { get; init; } = [];
    public IReadOnlyList<string> LearnSkills { get; init; } = [];
    public IReadOnlyList<string> ExpertSkills { get; init; } = [];
}

public sealed class AdminUserStatusFormViewModel
{
    public Guid Id { get; set; }
    [Required] public AccountStatus Status { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class AdminExpertApplicationsViewModel
{
    public string Status { get; init; } = "all";
    public int UnderReviewCount { get; init; }
    public int ApprovedCount { get; init; }
    public int NeedsInformationCount { get; init; }
    public int RejectedCount { get; init; }
    public IReadOnlyList<AdminExpertApplicationRowViewModel> Applications { get; init; } = [];
}

public sealed record AdminExpertApplicationRowViewModel(int Id, Guid UserId, string UserName, string Email,
    string SkillName, string Experience, string TeachingApproach, string? PortfolioUrl, string? SampleUrl,
    ExpertApplicationStatus Status, string? AdminReason, DateTime SubmittedAtUtc, DateTime? ReviewedAtUtc);

public sealed class ExpertDecisionViewModel
{
    public int Id { get; set; }
    [Required] public ExpertApplicationStatus Decision { get; set; }
    [Required, StringLength(1500, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
}
