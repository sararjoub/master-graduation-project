namespace SkillsSwap.Models;

public sealed class SessionScheduleInput
{
    public string? Title { get; set; }
    public string? Objectives { get; set; }
    public string? Notes { get; set; }
    public string? LocalStart { get; set; }
    public string? TimeZoneId { get; set; }
    public int DurationMinutes { get; set; }
    public string? Reason { get; set; }
    public string? Version { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public sealed record ScheduleProposalRow(long Id, int Revision, DateTimeOffset Start, int Duration,
    ScheduleProposalStatus Status, bool IsMine);
public sealed record SessionRow(long Id, long RequestId, string? Title, string Skill, string Partner, string? Objectives,
    string? Notes, DateTimeOffset Start, int Duration, SessionStatus Status, string? CancellationReason,
    IReadOnlyList<ScheduleProposalRow> Proposals, byte[] Version, bool IsTeacher, bool IsActive,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<AbsenceRow> Absences, IReadOnlyList<ScheduleHistoryRow> History, SessionReviewRow? Review, string? MeetingRoom = null, string? LegacyMeetingUrl = null);
public sealed record PeerSessionPage(long? RequestId, bool CanPropose, int Page, bool HasNext,
    IReadOnlyList<SessionRow> Sessions, DateTimeOffset Now)
{
    public bool IsTeacher { get; init; }
    public bool RelationshipActive { get; init; }
    public IReadOnlyList<SessionRequestRow> Requests { get; init; } = [];
    public string? FormError { get; init; }
    public SessionScheduleInput? PreviousInput { get; init; }
    public string? PreviousAction { get; init; }
    public long? PreviousItemId { get; init; }
}
public sealed record SessionRequestRow(long Id, LearningSessionRequestStatus Status, DateTimeOffset CreatedAt, long? SessionId);
public sealed record AbsenceRow(long Id, string Reason, DateTimeOffset CreatedAt, DateTimeOffset ForStart, SessionAbsenceRequestStatus Status);
public sealed record ScheduleHistoryRow(DateTimeOffset PreviousStart, DateTimeOffset NewStart, int PreviousDuration, int NewDuration, DateTimeOffset ChangedAt);
public sealed record SessionReviewRow(byte Rating, string? Comment, DateTime CreatedAt);
public sealed record DashboardSessionRow(long Id, string Title, DateTimeOffset Start);
public sealed record DashboardSchedulingRequestRow(long RelationshipId, string Skill);
