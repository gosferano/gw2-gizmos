namespace Gw2Gizmos.Data.Static.Events;

/// <summary>
/// Hardcoded map-meta schedule (UTC), computed from the GW2 wiki event-timer data. Each entry points at the
/// map's notable boss/peak phase (Octovine, Chak Gerent, Soo-Won, …), carries its waypoint chat link, and
/// mostly runs on a clean 2-hour cycle (a couple are hourly). Times were derived deterministically from the
/// wiki's phase data and cross-checked against the world-boss anchor.
/// </summary>
public static class MetaEvents
{
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);
    private static readonly TimeSpan FourHours = TimeSpan.FromHours(4);
    private static readonly TimeSpan SixHours = TimeSpan.FromHours(6);

    public static readonly IReadOnlyList<ScheduledEvent> All = new[]
    {
        // Core Tyria — the Ley-Line Anomaly and Fractal Incursion each rotate through fixed maps, so each
        // location is its own entry (the screen surfaces whichever comes next, with its map + chat link).
        Meta("ley-line-timberline-falls", "Ley-Line Anomaly", "Timberline Falls", "[&BEwCAAA=]", Schedule.Every(SixHours, T("00:20")), 20, Expansion.CoreTyria),
        Meta("ley-line-iron-marches", "Ley-Line Anomaly", "Iron Marches", "[&BOcBAAA=]", Schedule.Every(SixHours, T("02:20")), 20, Expansion.CoreTyria),
        Meta("ley-line-gendarran-fields", "Ley-Line Anomaly", "Gendarran Fields", "[&BOQAAAA=]", Schedule.Every(SixHours, T("04:20")), 20, Expansion.CoreTyria),
        Meta("fractal-incursion-kessex-hills", "Fractal Incursion", "Kessex Hills", "[&BBIAAAA=]", Schedule.Every(FourHours, T("00:00")), 15, Expansion.CoreTyria),
        Meta("fractal-incursion-diessa-plateau", "Fractal Incursion", "Diessa Plateau", "[&BN0AAAA=]", Schedule.Every(FourHours, T("01:00")), 15, Expansion.CoreTyria),
        Meta("fractal-incursion-brisban-wildlands", "Fractal Incursion", "Brisban Wildlands", "[&BHUAAAA=]", Schedule.Every(FourHours, T("02:00")), 15, Expansion.CoreTyria),
        Meta("fractal-incursion-snowden-drifts", "Fractal Incursion", "Snowden Drifts", "[&BLQAAAA=]", Schedule.Every(FourHours, T("03:00")), 15, Expansion.CoreTyria),

        // Living World Season 2 (Dry Top) + Heart of Thorns
        Meta("sandstorm", "Sandstorm", "Dry Top", "[&BIAHAAA=]", Schedule.Every(Hour, T("00:40")), 20, Expansion.LivingWorldSeason2),
        Meta("verdant-brink", "Night Bosses", "Verdant Brink", "[&BAgIAAA=]", Schedule.Every(TwoHours, T("00:10")), 20, Expansion.HeartOfThorns),
        Meta("octovine", "Octovine", "Auric Basin", "[&BAIIAAA=]", Schedule.Every(TwoHours, T("01:00")), 20, Expansion.HeartOfThorns),
        Meta("chak-gerent", "Chak Gerent", "Tangled Depths", "[&BPUHAAA=]", Schedule.Every(TwoHours, T("00:30")), 20, Expansion.HeartOfThorns),
        Meta("dragons-stand", "Dragon's Stand", "Dragon's Stand", "[&BBAIAAA=]", Schedule.Every(TwoHours, T("01:30")), 90, Expansion.HeartOfThorns),

        // Path of Fire
        Meta("casino-blitz", "Casino Blitz", "Crystal Oasis", "[&BLsKAAA=]", Schedule.Every(TwoHours, T("00:05")), 16, Expansion.PathOfFire),
        Meta("buried-treasure", "Buried Treasure", "Desert Highlands", "[&BGsKAAA=]", Schedule.Every(TwoHours, T("01:00")), 20, Expansion.PathOfFire),
        Meta("doppelganger", "Doppelganger", "Elon Riverlands", "[&BFMKAAA=]", Schedule.Every(TwoHours, T("01:55")), 20, Expansion.PathOfFire),
        Meta("junundu-rising", "Junundu Rising", "The Desolation", "[&BMEKAAA=]", Schedule.Every(Hour, T("00:30")), 20, Expansion.PathOfFire),
        Meta("serpents-ire", "Serpents' Ire", "Domain of Vabbi", "[&BHQKAAA=]", Schedule.Every(TwoHours, T("00:30")), 30, Expansion.PathOfFire),

        // Living World Season 4 (Istan / Jahai / Thunderhead) + The Icebrood Saga (Grothmar / Bjora)
        Meta("palawadan", "Palawadan", "Domain of Istan", "[&BAkLAAA=]", Schedule.Every(TwoHours, T("01:45")), 30, Expansion.LivingWorldSeason4),
        Meta("death-branded-shatterer", "Death-Branded Shatterer", "Jahai Bluffs", "[&BJMLAAA=]", Schedule.Every(TwoHours, T("01:15")), 15, Expansion.LivingWorldSeason4),
        Meta("oil-floes", "The Oil Floes", "Thunderhead Peaks", "[&BKYLAAA=]", Schedule.Every(TwoHours, T("00:45")), 15, Expansion.LivingWorldSeason4),
        Meta("metal-concert", "Metal Concert", "Grothmar Valley", "[&BPgLAAA=]", Schedule.Every(TwoHours, T("01:40")), 20, Expansion.IcebroodSaga),
        Meta("drakkar", "Drakkar", "Bjora Marches", "[&BDkMAAA=]", Schedule.Every(TwoHours, T("01:05")), 35, Expansion.IcebroodSaga),

        // End of Dragons
        Meta("aetherblade-assault", "Aetherblade Assault", "Seitung Province", "[&BGUNAAA=]", Schedule.Every(TwoHours, T("01:30")), 30, Expansion.EndOfDragons),
        Meta("kaineng-blackout", "Kaineng Blackout", "New Kaineng City", "[&BBkNAAA=]", Schedule.Every(TwoHours, T("00:00")), 40, Expansion.EndOfDragons),
        Meta("aspenwood", "Aspenwood", "The Echovald Wilds", "[&BPkMAAA=]", Schedule.Every(TwoHours, T("01:40")), 20, Expansion.EndOfDragons),
        Meta("dragons-end", "The Battle for the Jade Sea", "Dragon's End", "[&BKIMAAA=]", Schedule.Every(TwoHours, T("01:00")), 60, Expansion.EndOfDragons),

        // Secrets of the Obscure
        Meta("skywatch", "Unlock the Wizard's Tower", "Skywatch Archipelago", "[&BL4NAAA=]", Schedule.Every(TwoHours, T("01:00")), 25, Expansion.SecretsOfTheObscure),
        Meta("wizards-tower", "Wizard's Tower", "Wizard's Tower", "[&BB8OAAA=]", Schedule.Every(TwoHours, T("01:40")), 25, Expansion.SecretsOfTheObscure),
        Meta("amnytas", "Defense of Amnytas", "Amnytas", "[&BDQOAAA=]", Schedule.Every(TwoHours, T("00:00")), 25, Expansion.SecretsOfTheObscure),

        // Janthir Wilds
        Meta("mists-and-monsters", "Of Mists and Monsters", "Janthir Syntri", "[&BCoPAAA=]", Schedule.Every(TwoHours, T("00:30")), 25, Expansion.JanthirWilds),
        Meta("titanic-voyage", "A Titanic Voyage", "Bava Nisos", "[&BGEPAAA=]", Schedule.Every(TwoHours, T("01:20")), 25, Expansion.JanthirWilds),

        // Visions of Eternity
        Meta("hammerhart-rumble", "Hammerhart Rumble", "Shipwreck Strand", "[&BJEPAAA=]", Schedule.Every(TwoHours, T("00:40")), 20, Expansion.VisionsOfEternity),
        Meta("starlit-weald", "Secrets of the Weald", "Starlit Weald", "[&BJ4PAAA=]", Schedule.Every(TwoHours, T("01:40")), 35, Expansion.VisionsOfEternity),
        Meta("shackles-of-the-ancients", "Shackles of the Ancients", "Eternity's Garden", "[&BPwPAAA=]", Schedule.Every(TwoHours, T("01:10")), 25, Expansion.VisionsOfEternity),
    };

    private static TimeSpan T(string time) => TimeSpan.Parse(time);

    private static ScheduledEvent Meta(
        string id,
        string name,
        string map,
        string chatLink,
        IReadOnlyList<TimeSpan> times,
        int durationMinutes,
        Expansion expansion
    ) =>
        new()
        {
            Id = id,
            Name = name,
            Map = map,
            Kind = EventKind.MetaEvent,
            Expansion = expansion,
            ChatLink = chatLink,
            Duration = TimeSpan.FromMinutes(durationMinutes),
            DailyTimesUtc = times
        };
}
