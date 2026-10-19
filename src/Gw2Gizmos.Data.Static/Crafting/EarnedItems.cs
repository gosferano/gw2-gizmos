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
    };
}
