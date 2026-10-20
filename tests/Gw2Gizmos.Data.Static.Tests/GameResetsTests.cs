namespace Gw2Gizmos.Data.Static.Tests;

/// <summary>
/// The game's resets: the daily at 00:00 UTC and the weekly on Monday at 07:30 UTC — the next of either, and the
/// latest and next of each.
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

    [Theory]
    [InlineData(20, 15, 0, 19, 7, 30)] // Tuesday → that Monday's weekly
    [InlineData(26, 7, 29, 19, 7, 30)] // Monday just before the weekly → the previous Monday's
    [InlineData(26, 7, 30, 26, 7, 30)] // Monday at the weekly → that one
    [InlineData(25, 23, 59, 19, 7, 30)] // Sunday night → the Monday before
    public void The_last_weekly_reset_is_the_latest_monday_morning_at_or_before_now(
        int day, int hour, int minute, int lastDay, int lastHour, int lastMinute)
    {
        Assert.Equal(Utc(lastDay, lastHour, lastMinute), GameResets.LastWeekly(Utc(day, hour, minute)));
        Assert.Equal(Utc(lastDay, lastHour, lastMinute).AddDays(7), GameResets.NextWeekly(Utc(day, hour, minute)));
    }

    [Fact]
    public void The_last_daily_reset_is_the_current_utc_day_start()
    {
        // 22:30 UTC on the 20th.
        var helsinkiEarlyMorning = new DateTimeOffset(2026, 10, 21, 1, 30, 0, TimeSpan.FromHours(3));

        Assert.Equal(Utc(20, 0), GameResets.LastDaily(helsinkiEarlyMorning));
        Assert.Equal(Utc(21, 0), GameResets.NextDaily(helsinkiEarlyMorning));
    }
}
