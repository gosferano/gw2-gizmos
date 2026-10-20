using System.Globalization;

namespace Gw2Gizmos.Data.Static.Resets;

/// <summary>
/// What the API's once-per-reset activity ids (world bosses, map chests, daily crafts, raid encounters) stand for. The
/// API gives only ids such as "tequatl_the_sunless", without names, so names, the matching event on the Events page's
/// schedule and a daily craft's item are kept here by hand. An id missing here (e.g. one a game update added) still
/// shows, named from the id itself.
/// </summary>
public static class ResetActivities
{
    /// <summary>
    /// The world boss each <c>/v2/worldbosses</c> id is, by its <see cref="Events.ScheduledEvent"/> id.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> WorldBossEvents = new Dictionary<string, string>
    {
        ["admiral_taidha_covington"] = "admiral-taidha-covington",
        ["claw_of_jormag"] = "claw-of-jormag",
        ["drakkar"] = "drakkar",
        ["fire_elemental"] = "fire-elemental",
        ["great_jungle_wurm"] = "great-jungle-wurm",
        ["inquest_golem_mark_ii"] = "golem-mark-ii",
        ["karka_queen"] = "karka-queen",
        ["megadestroyer"] = "megadestroyer",
        ["mists_and_monsters_titans"] = "mists-and-monsters",
        ["modniir_ulgoth"] = "modniir-ulgoth",
        ["shadow_behemoth"] = "shadow-behemoth",
        ["svanir_shaman_chief"] = "svanir-shaman-chief",
        ["tequatl_the_sunless"] = "tequatl",
        ["the_shatterer"] = "the-shatterer",
        ["triple_trouble_wurm"] = "triple-trouble",
    };

    /// <summary>
    /// The map each <c>/v2/mapchests</c> id is the meta reward chest of (the ids are the map's name followed by
    /// "_heros_choice_chest").
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> MapChestMaps = new Dictionary<string, string>
    {
        ["amnytas_heros_choice_chest"] = "Amnytas",
        ["auric_basin_heros_choice_chest"] = "Auric Basin",
        ["citadel_of_zakiros_heros_choice_chest"] = "Citadel of Zakiros",
        ["convergence_heros_choice_chest"] = "Convergences",
        ["crystal_oasis_heros_choice_chest"] = "Crystal Oasis",
        ["domain_of_vabbi_heros_choice_chest"] = "Domain of Vabbi",
        ["dragons_end_heros_choice_chest"] = "Dragon's End",
        ["dragons_stand_heros_choice_chest"] = "Dragon's Stand",
        ["echovald_wilds_heros_choice_chest"] = "The Echovald Wilds",
        ["elon_riverlands_heros_choice_chest"] = "Elon Riverlands",
        ["gyala_delve_heros_choice_chest"] = "Gyala Delve",
        ["inner_nayos_heros_choice_chest"] = "Inner Nayos",
        ["new_kaineng_city_heros_choice_chest"] = "New Kaineng City",
        ["seitung_province_heros_choice_chest"] = "Seitung Province",
        ["skywatch_archipelago_heros_choice_chest"] = "Skywatch Archipelago",
        ["tangled_depths_heros_choice_chest"] = "Tangled Depths",
        ["the_desolation_heros_choice_chest"] = "The Desolation",
        ["verdant_brink_heros_choice_chest"] = "Verdant Brink",
        ["wild_island_heros_choice_chest"] = "Wild Island",
    };

    /// <summary>The meta whose reward each map chest is, by its <see cref="Events.ScheduledEvent"/> id, where the
    /// Events page schedules it.</summary>
    public static readonly IReadOnlyDictionary<string, string> MapChestEvents = new Dictionary<string, string>
    {
        ["amnytas_heros_choice_chest"] = "amnytas",
        ["auric_basin_heros_choice_chest"] = "octovine",
        ["convergence_heros_choice_chest"] = "convergence-outer-nayos",
        ["crystal_oasis_heros_choice_chest"] = "casino-blitz",
        ["domain_of_vabbi_heros_choice_chest"] = "serpents-ire",
        ["dragons_end_heros_choice_chest"] = "dragons-end",
        ["dragons_stand_heros_choice_chest"] = "dragons-stand",
        ["echovald_wilds_heros_choice_chest"] = "aspenwood",
        ["elon_riverlands_heros_choice_chest"] = "doppelganger",
        ["new_kaineng_city_heros_choice_chest"] = "kaineng-blackout",
        ["seitung_province_heros_choice_chest"] = "aetherblade-assault",
        ["skywatch_archipelago_heros_choice_chest"] = "skywatch",
        ["tangled_depths_heros_choice_chest"] = "chak-gerent",
        ["the_desolation_heros_choice_chest"] = "junundu-rising",
        ["verdant_brink_heros_choice_chest"] = "verdant-brink",
    };

    /// <summary>
    /// The map chests whose meta isn't on a timer (it runs whenever players get it going), so they can be done any
    /// time rather than at a scheduled start.
    /// </summary>
    public static readonly IReadOnlySet<string> UntimedMapChests = new HashSet<string>
    {
        "citadel_of_zakiros_heros_choice_chest",
        "gyala_delve_heros_choice_chest",
        "inner_nayos_heros_choice_chest",
        "wild_island_heros_choice_chest",
    };

    /// <summary>The item each <c>/v2/dailycrafting</c> id crafts.</summary>
    public static readonly IReadOnlyDictionary<string, int> DailyCraftingItems = new Dictionary<string, int>
    {
        ["charged_quartz_crystal"] = 43772,
        ["glob_of_elder_spirit_residue"] = 46744,
        ["lump_of_mithrilium"] = 46742,
        ["spool_of_silk_weaving_thread"] = 46740,
        ["spool_of_thick_elonian_cord"] = 46745,
    };

    /// <summary>Raid wings and encounters whose name isn't their id spelled out.</summary>
    private static readonly IReadOnlyDictionary<string, string> RaidNames = new Dictionary<string, string>
    {
        // The encounter is the fight with Dhuum, the name players know it by.
        ["voice_in_the_void"] = "Dhuum",
    };

    /// <summary>A raid wing's or encounter's name, e.g. "Spirit Vale" for "spirit_vale".</summary>
    public static string RaidName(string id) => RaidNames.GetValueOrDefault(id) ?? NameFromId(id);

    /// <summary>
    /// A readable name made from an id: words capitalised, except "of", "the", "in" and "and" after the first, e.g.
    /// "The Key of Ahdashim" for "the_key_of_ahdashim".
    /// </summary>
    public static string NameFromId(string id)
    {
        string[] words = id.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (i == 0 || words[i] is not ("of" or "the" or "in" or "and"))
            {
                words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(words[i]);
            }
        }

        return string.Join(' ', words);
    }
}
