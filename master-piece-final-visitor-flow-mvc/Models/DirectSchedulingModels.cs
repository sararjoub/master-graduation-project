using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public enum LearningSessionRequestStatus : byte
{
    Pending = 0, Fulfilled = 1, Withdrawn = 2, ClosedWithRelationship = 3
}

public enum SessionAbsenceRequestStatus : byte
{
    Pending = 0, Withdrawn = 1, ResolvedByCancellation = 2, ResolvedByReschedule = 3, ExpiredUnresolved = 4
}

public sealed class LearningSessionRequest
{
    public long Id { get; set; }
    public long LearningRequestId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public LearningSessionRequestStatus Status { get; set; }
    public long? FulfilledSessionId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

public sealed class SessionAbsenceRequest
{
    public long Id { get; set; }
    public long LearningSessionId { get; set; }
    public Guid RequestedByUserId { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset RequestedForStartsAtUtc { get; set; }
    public SessionAbsenceRequestStatus Status { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

public sealed class SessionScheduleHistory
{
    public long Id { get; set; }
    public long LearningSessionId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset PreviousStartsAtUtc { get; set; }
    public DateTimeOffset NewStartsAtUtc { get; set; }
    public int PreviousDurationMinutes { get; set; }
    public int NewDurationMinutes { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}
