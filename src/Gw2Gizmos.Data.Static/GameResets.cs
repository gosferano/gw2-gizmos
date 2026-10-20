namespace Gw2Gizmos.Data.Static;

/// <summary>
/// The game's fixed resets, which the API doesn't publish: the daily at 00:00 UTC and the weekly on Monday at 07:30
/// UTC. Rotating achievement categories (dailies, weeklies), the Wizard's Vault and once-per-reset rewards (world
/// bosses, map chests, daily crafting, raid clears) change at these.
/// </summary>
public static class GameResets
{
    private static readonly TimeSpan WeeklyTimeOfDay = new(7, 30, 0);

    /// <summary>The first daily or weekly reset after <paramref name="now"/>.</summary>
    public static DateTimeOffset Next(DateTimeOffset now)
    {
        DateTimeOffset daily = NextDaily(now);
        DateTimeOffset weekly = NextWeekly(now);
        return weekly < daily ? weekly : daily;
    }

    /// <summary>The first daily reset after <paramref name="now"/>.</summary>
    public static DateTimeOffset NextDaily(DateTimeOffset now) => LastDaily(now).AddDays(1);

    /// <summary>The latest daily reset at or before <paramref name="now"/>: the current day's start.</summary>
    public static DateTimeOffset LastDaily(DateTimeOffset now) =>
        new(now.ToUniversalTime().Date, TimeSpan.Zero);

    /// <summary>The first weekly reset after <paramref name="now"/>.</summary>
    public static DateTimeOffset NextWeekly(DateTimeOffset now) => LastWeekly(now).AddDays(7);

    /// <summary>The latest weekly reset at or before <paramref name="now"/>: the current week's start.</summary>
    public static DateTimeOffset LastWeekly(DateTimeOffset now)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        int daysSinceMonday = ((int)utc.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekly = new DateTimeOffset(utc.Date.AddDays(-daysSinceMonday) + WeeklyTimeOfDay, TimeSpan.Zero);
        return weekly > utc ? weekly.AddDays(-7) : weekly;
    }
}
