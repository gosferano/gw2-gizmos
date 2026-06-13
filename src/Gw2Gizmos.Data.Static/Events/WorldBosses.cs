namespace Gw2Gizmos.Data.Static.Events;

/// <summary>
/// Hardcoded world-boss spawn schedule (UTC), from the GW2 wiki world-boss timetable. The standard
/// core-Tyria bosses run on clean 2h or 3h cycles (expressed with <see cref="Schedule.Every"/>); the three
/// "hardcore" bosses have irregular six-per-day times (explicit via <see cref="Schedule.At"/>). Each entry
/// carries the boss's waypoint chat link.
/// </summary>
public static class WorldBosses
{
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);
    private static readonly TimeSpan ThreeHours = TimeSpan.FromHours(3);

    public static readonly IReadOnlyList<ScheduledEvent> All = new[]
    {
        // Standard bosses — every 2 hours.
        Boss("svanir-shaman-chief", "Svanir Shaman Chief", "Wayfarer Foothills", "[&BMIDAAA=]", Schedule.Every(TwoHours, T("00:15"))),
        Boss("fire-elemental", "Fire Elemental", "Metrica Province", "[&BEcAAAA=]", Schedule.Every(TwoHours, T("00:45"))),
        Boss("great-jungle-wurm", "Great Jungle Wurm", "Caledon Forest", "[&BEEFAAA=]", Schedule.Every(TwoHours, T("01:15"))),
        Boss("shadow-behemoth", "Shadow Behemoth", "Queensdale", "[&BPcAAAA=]", Schedule.Every(TwoHours, T("01:45"))),

        // Standard bosses — every 3 hours.
        Boss("admiral-taidha-covington", "Admiral Taidha Covington", "Bloodtide Coast", "[&BKgBAAA=]", Schedule.Every(ThreeHours, T("00:00"))),
        Boss("megadestroyer", "Megadestroyer", "Mount Maelstrom", "[&BM0CAAA=]", Schedule.Every(ThreeHours, T("00:30"))),
        Boss("the-shatterer", "The Shatterer", "Blazeridge Steppes", "[&BE4DAAA=]", Schedule.Every(ThreeHours, T("01:00"))),
        Boss("modniir-ulgoth", "Modniir Ulgoth", "Harathi Hinterlands", "[&BLAAAAA=]", Schedule.Every(ThreeHours, T("01:30"))),
        Boss("golem-mark-ii", "Golem Mark II", "Mount Maelstrom", "[&BNQCAAA=]", Schedule.Every(ThreeHours, T("02:00"))),
        Boss("claw-of-jormag", "Claw of Jormag", "Frostgorge Sound", "[&BHoCAAA=]", Schedule.Every(ThreeHours, T("02:30"))),

        // Hardcore bosses — six per day at irregular times.
        Boss("tequatl", "Tequatl the Sunless", "Sparkfly Fen", "[&BNABAAA=]", Schedule.At("00:00", "03:00", "07:00", "11:30", "16:00", "19:00")),
        Boss("triple-trouble", "Triple Trouble", "Bloodtide Coast", "[&BKoBAAA=]", Schedule.At("01:00", "04:00", "08:00", "12:30", "17:00", "20:00")),
        Boss("karka-queen", "Karka Queen", "Southsun Cove", "[&BNUGAAA=]", Schedule.At("02:00", "06:00", "10:30", "15:00", "18:00", "23:00")),
    };

    private static TimeSpan T(string time) => TimeSpan.Parse(time);

    private static ScheduledEvent Boss(string id, string name, string map, string chatLink, IReadOnlyList<TimeSpan> times) =>
        new()
        {
            Id = id,
            Name = name,
            Map = map,
            Kind = EventKind.WorldBoss,
            Expansion = Expansion.CoreTyria,
            ChatLink = chatLink,
            Duration = TimeSpan.FromMinutes(15),
            DailyTimesUtc = times
        };
}
