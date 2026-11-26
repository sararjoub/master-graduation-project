using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed class AutomaticSessionCompletionTests
{
    [Fact]
    public void Video_access_ends_at_the_scheduled_end_even_before_the_background_sweep()
    {
        var startsAt = new DateTimeOffset(2090, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var endsAt = startsAt.AddMinutes(30);
        var settings = new VideoMeetingOptions();

        Assert.Equal("Meeting is available.", settings.Availability(SessionStatus.Scheduled, true,
            startsAt, 30, endsAt.AddTicks(-1)));
        Assert.Equal("The scheduled session time has ended.", settings.Availability(SessionStatus.Scheduled,
            true, startsAt, 30, endsAt));
        Assert.Equal("Meeting is available.", settings.Availability(SessionStatus.Scheduled, true,
            startsAt.AddMinutes(20), 30, endsAt));
        Assert.NotEqual("Meeting is available.", settings.Availability(SessionStatus.Completed, true,
            startsAt, 30, endsAt));
    }

    [Fact]
    public void Old_manual_completion_action_is_rejected()
    {
        Assert.False(PeerSessionService.IsAction("complete"));
        Assert.True(PeerSessionService.IsAction("review"));
    }
}
