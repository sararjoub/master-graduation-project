using System.Text.RegularExpressions;

namespace SkillsSwap.Models;

public sealed class VideoMeetingOptions
{
    public bool Enabled { get; set; } = true;
    public string Domain { get; set; } = "meet.jit.si";
    public int OpeningMinutes { get; set; } = 10;

    public string Availability(SessionStatus status, bool active, DateTimeOffset start, int duration,
        DateTimeOffset now, string? room = null, string? legacyUrl = null)
    {
        if (!Enabled || !active || status != SessionStatus.Scheduled || legacyUrl is not null ||
            (room is not null && !Regex.IsMatch(room, @"\Askillswap-jitsi-[0-9a-f]{32}\z")))
            return "Video meeting is unavailable for this session.";
        var elapsed = (now - start).TotalMinutes;
        if (elapsed < -OpeningMinutes) return $"Video access opens {OpeningMinutes} minutes before the session.";
        if (now - start >= TimeSpan.FromMinutes(duration)) return "The scheduled session time has ended.";
        return "Meeting is available.";
    }
}

public sealed record VideoMeetingPage(long Id, string Title, string Skill, string Teacher, string Learner,
    DateTimeOffset StartsAt, int Duration, string DisplayName, string? Room, string Domain, string Availability);
