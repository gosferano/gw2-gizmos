namespace Gw2Gizmos.Data.Static;

/// <summary>
/// The game's fixed resets, which the API doesn't publish: the daily at 00:00 UTC and the weekly on Monday at 07:30
/// UTC. Rotating achievement categories (dailies, weeklies) and their progress change at these.
/// </summary>
public static class GameResets
{
    private static readonly TimeSpan WeeklyTimeOfDay = new(7, 30, 0);

    /// <summary>The first daily or weekly reset after <paramref name="now"/>.</summary>
    public static DateTimeOffset Next(DateTimeOffset now)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        var daily = new DateTimeOffset(utc.Date.AddDays(1), TimeSpan.Zero);

        int daysToMonday = ((int)DayOfWeek.Monday - (int)utc.DayOfWeek + 7) % 7;
        var weekly = new DateTimeOffset(utc.Date.AddDays(daysToMonday) + WeeklyTimeOfDay, TimeSpan.Zero);
        if (weekly <= utc)
        {
            weekly = weekly.AddDays(7);
        }

        return weekly < daily ? weekly : daily;
    }
}
