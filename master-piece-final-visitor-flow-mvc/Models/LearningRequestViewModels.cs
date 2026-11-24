using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public sealed class PeerRequestInput
{
    public Guid TeacherId { get; set; }
    public int SkillId { get; set; }
    [Required, StringLength(1500)]
    public string Message { get; set; } = "";
}

public sealed record PeerRequestTarget(string TeacherName, string SkillName, string SkillSlug);
public sealed record PeerRequestForm(PeerRequestInput Input, PeerRequestTarget Target, long? ActiveRequestId);
public sealed record PeerRequestRow(long Id, string Person, string Skill, string Message,
    DateTime Created, DateTime? Responded, RequestStatus Status, bool Incoming, string? ProfileImageUrl);

public sealed record PeerRequestList(IReadOnlyList<PeerRequestRow> Rows, string Tab,
    int Page, bool HasNext, int IncomingPending, int OutgoingPending, string Search, RequestStatus? Status);
