namespace Gw2Gizmos.Data.Static.Events;

/// <summary>
/// Hardcoded schedule (UTC) for the recurring core-Tyria invasions (from the wiki event-timer data): the
/// Awakened Invasion (hourly) and Scarlet's Minions (every two hours). They roam across core maps, so they
/// have no single fixed map; the Scarlet's entry carries the chat link the wiki lists.
/// </summary>
public static class Invasions
{
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);

    public static readonly IReadOnlyList<ScheduledEvent> All = new[]
    {
        Invasion("awakened-invasion", "Awakened Invasion", "Core Tyria", null, Schedule.Every(Hour, T("00:30")), 20),
        Invasion("scarlets-minions", "Scarlet's Minions", "Core Tyria", "[&BOQAAAA=]", Schedule.Every(TwoHours, T("01:00")), 20),
    };

    private static TimeSpan T(string time) => TimeSpan.Parse(time);

    private static ScheduledEvent Invasion(string id, string name, string map, string? chatLink, IReadOnlyList<TimeSpan> times, int durationMinutes) =>
        new()
        {
            Id = id,
            Name = name,
            Map = map,
            Kind = EventKind.Invasion,
            ChatLink = chatLink,
            Duration = TimeSpan.FromMinutes(durationMinutes),
            DailyTimesUtc = times
        };
}
