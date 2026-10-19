namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>
/// The next game reset: the daily at 00:00 UTC, or the weekly on Monday at 07:30 UTC, whichever is first.
/// </summary>
public sealed class GameResetsTests
{
    private static DateTimeOffset Utc(int day, int hour, int minute = 0) =>
        new(2026, 10, day, hour, minute, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(20, 15, 0, 21, 0, 0)] // Tuesday afternoon → Wednesday's daily
    [InlineData(25, 23, 59, 26, 0, 0)] // Sunday just before midnight → Monday's daily
    [InlineData(26, 0, 0, 26, 7, 30)] // Monday at the daily → the weekly that morning
    [InlineData(26, 7, 30, 27, 0, 0)] // Monday at the weekly → Tuesday's daily
    [InlineData(26, 3, 0, 26, 7, 30)] // Monday before the weekly → the weekly
    public void The_next_reset_is_the_first_daily_or_weekly_after_now(
        int day, int hour, int minute, int nextDay, int nextHour, int nextMinute)
    {
        Assert.Equal(Utc(nextDay, nextHour, nextMinute), GameResets.Next(Utc(day, hour, minute)));
    }

    [Fact]
    public void A_local_time_is_taken_as_the_same_instant()
    {
        var helsinkiEvening = new DateTimeOffset(2026, 10, 20, 23, 30, 0, TimeSpan.FromHours(3)); // 20:30 UTC

        Assert.Equal(Utc(21, 0), GameResets.Next(helsinkiEvening));
    }
}
