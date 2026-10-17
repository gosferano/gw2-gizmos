namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Session figures count only time in the world, not character select or a minimised game.</summary>
public sealed class PlayedTimeTests
{
    private static readonly DateTimeOffset Read = new(2026, 10, 17, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_running_session_keeps_counting_only_while_in_the_world()
    {
        var inWorld = new SessionValueSummary(0, 0, Read.AddHours(-2), null, TimeSpan.FromMinutes(90), Read, InWorld: true);
        var away = inWorld with { InWorld = false };

        Assert.Equal(TimeSpan.FromMinutes(100), inWorld.PlayedTime(Read.AddMinutes(10)));
        Assert.Equal(TimeSpan.FromMinutes(90), away.PlayedTime(Read.AddMinutes(10)));
    }

    [Fact]
    public void Gold_per_hour_is_over_the_time_played()
    {
        // 30 gold over 1.5 hours played (in a 2-hour sitting) is 20 gold an hour.
        var ended = new SessionValueSummary(300_000, 0, Read.AddHours(-2), Read, TimeSpan.FromMinutes(90), Read);

        Assert.Equal(200_000, ended.GoldPerHourCopper);
    }

    [Fact]
    public void A_segment_shows_its_time_played_and_any_time_away()
    {
        SessionSegmentRow Row(TimeSpan away) =>
            new(1, 0, "Alice", Read, null, 0, 0, 0, 0, TimeSpan.FromMinutes(45), away);

        Assert.Equal("45m (12m away)", Row(TimeSpan.FromMinutes(12)).DurationDisplay);
        Assert.Equal("45m", Row(TimeSpan.FromSeconds(20)).DurationDisplay);
    }
}
