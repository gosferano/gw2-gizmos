namespace Gw2Gizmos.Data.Static.Events;

/// <summary>A recurring festival whose timed events the schedule includes (those the wiki's event timer lists).</summary>
public enum Festival
{
    FestivalOfTheFourWinds,
    DragonBash,
    Halloween
}

/// <summary>
/// Hardcoded schedule (UTC) for the festivals' timed events, from the GW2 wiki event-timer data (v5.4, 2026-09). They
/// only run while their festival is on, which nothing (the API included) reports — so the Events screen shows them,
/// and reminds about them, only for the festival the user has picked as running.
/// </summary>
public static class Festivals
{
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);

    private const string LabyrinthineCliffs = "[&BBwHAAA=]";

    public static readonly IReadOnlyList<ScheduledEvent> All = new[]
    {
        // Festival of the Four Winds — the Labyrinthine Cliffs activities, each every 2 hours.
        Event("four-winds-skiff-race", "Skiff Race", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("00:00")), 10, Festival.FestivalOfTheFourWinds),
        Event("four-winds-water-balloons", "Water Balloons", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("00:15")), 10, Festival.FestivalOfTheFourWinds),
        Event("four-winds-treasure-hunt", "Treasure Hunt", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("00:30")), 30, Festival.FestivalOfTheFourWinds),
        Event("four-winds-skimmer-race", "Skimmer Race", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("01:15")), 10, Festival.FestivalOfTheFourWinds),
        Event("four-winds-fishing", "Fishing Tournament", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("01:30")), 10, Festival.FestivalOfTheFourWinds),
        Event("four-winds-dolyak-race", "Dolyak Race", "Labyrinthine Cliffs", LabyrinthineCliffs, Schedule.Every(TwoHours, T("01:45")), 10, Festival.FestivalOfTheFourWinds),

        // Dragon Bash — the Hologram Stampede, hourly, rotating through four maps (each location its own entry).
        Event("dragon-bash-wayfarer-foothills", "Hologram Stampede", "Wayfarer Foothills", "[&BH0BAAA=]", Schedule.Every(Hour, T("00:00")), 5, Festival.DragonBash),
        Event("dragon-bash-dredgehaunt-cliffs", "Hologram Stampede", "Dredgehaunt Cliffs", "[&BGMCAAA=]", Schedule.Every(Hour, T("00:15")), 5, Festival.DragonBash),
        Event("dragon-bash-lornars-pass", "Hologram Stampede", "Lornar's Pass", "[&BJkBAAA=]", Schedule.Every(Hour, T("00:30")), 5, Festival.DragonBash),
        Event("dragon-bash-snowden-drifts", "Hologram Stampede", "Snowden Drifts", "[&BL4AAAA=]", Schedule.Every(Hour, T("00:45")), 5, Festival.DragonBash),

        // Halloween — Mad King Says in Lion's Arch's Grand Piazza, every 2 hours.
        Event("halloween-mad-king-says", "Mad King Says", "Lion's Arch", "[&BBAEAAA=]", Schedule.Every(TwoHours, T("00:00")), 10, Festival.Halloween),
    };

    /// <summary>The festival's name as players know it.</summary>
    public static string DisplayName(Festival festival) => festival switch
    {
        Festival.FestivalOfTheFourWinds => "Festival of the Four Winds",
        Festival.DragonBash => "Dragon Bash",
        Festival.Halloween => "Halloween",
        _ => festival.ToString(),
    };

    private static TimeSpan T(string time) => TimeSpan.Parse(time);

    private static ScheduledEvent Event(
        string id,
        string name,
        string map,
        string chatLink,
        IReadOnlyList<TimeSpan> times,
        int durationMinutes,
        Festival festival
    ) =>
        new()
        {
            Id = id,
            Name = name,
            Map = map,
            Kind = EventKind.Festival,
            // Every festival here runs in core-Tyria maps; the festival itself is what the screen filters on.
            Expansion = Expansion.CoreTyria,
            Festival = festival,
            ChatLink = chatLink,
            Duration = TimeSpan.FromMinutes(durationMinutes),
            DailyTimesUtc = times
        };
}
