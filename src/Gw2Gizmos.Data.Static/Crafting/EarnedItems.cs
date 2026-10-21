namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// Items earned by playing rather than bought, crafted or sold by a vendor: they cost no coin, only play time, so
/// the recipe engine counts them as obtainable for 0 instead of as dead-ends that leave a craft cost incomplete.
/// Each is kept with how it's earned, which the craft tree shows next to it.
/// </summary>
public static class EarnedItems
{
    /// <summary>How each earned item is obtained, e.g. "world completion", keyed by item id.</summary>
    public static readonly IReadOnlyDictionary<int, string> ByItemId = new Dictionary<int, string>
    {
        [19677] = "world completion", // Gift of Exploration — 2 per world completion
        [19678] = "a WvW reward track", // Gift of Battle — the Gift of Battle reward track

        // Account-bound materials with no vendor or recipe, got only by playing (per their wiki pages).
        [46735] = "open-world and dungeon chests", // Empyreal Fragment
        [46733] = "world boss and meta chests", // Dragonite Ore
        [36450] = "chests and achievement rewards", // Sip of Liquid Karma
        [45175] = "salvaging and chests", // Fine Essence of Luck
        [96410] = "drops in the Echovald Wilds", // Stained Glass Shard
        [66608] = "gathering and drops in the Maguuma Wastes", // Pile of Silky Sand
    };
}
