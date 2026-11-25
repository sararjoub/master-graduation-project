using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed class PeerSessionScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("2026-07-01T10:00", "UTC", 30)]
    [InlineData("2026-07-01T10:00", "America/New_York", 90)]
    public void Explicit_zone_and_valid_duration_produce_utc(string local, string zone, int duration)
    {
        Assert.True(PeerSessionService.TrySchedule(local, zone, duration, Now, out var start));
        Assert.Equal(TimeSpan.Zero, start.Offset);
        Assert.Equal(zone == "UTC" ? 10 : 14, start.Hour);
    }

    [Theory]
    [InlineData("2025-01-01T10:00", "UTC", 30)]
    [InlineData("2026-07-01T10:00", "UTC", 31)]
    [InlineData("2026-07-01T10:00", "not-a-zone", 45)]
    [InlineData("2026-02-30T10:00", "UTC", 45)]
    [InlineData("2026-03-08T02:30", "America/New_York", 60)]
    [InlineData("2026-11-01T01:30", "America/New_York", 60)]
    public void Past_invalid_ambiguous_and_nonexistent_inputs_are_rejected(string local, string zone, int duration) =>
        Assert.False(PeerSessionService.TrySchedule(local, zone, duration, Now, out _));
}
