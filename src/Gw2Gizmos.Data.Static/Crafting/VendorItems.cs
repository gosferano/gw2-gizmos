namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// Hardcoded NPC vendor prices — the GW2 API has no vendor <em>buy</em> price endpoint, so common vendor mats
/// are curated here. The recipe engine uses these as a price fallback when an item isn't listed on the trading
/// post, so vendor-bought ingredients aren't valued at 0.
/// Curate from the wiki — verify each price in-game before trusting it.
/// </summary>
public static class VendorItems
{
    public static readonly IReadOnlyList<VendorItem> All = new VendorItem[]
    {
        Coin(itemId: 46747, copper: 1496, quantity: 10, npc: "Master Craftsman"), // Thermocatalytic Reagent
    };

    /// <summary>Vendor items keyed by item id, for the engine's price-fallback lookup.</summary>
    public static readonly IReadOnlyDictionary<int, VendorItem> ByItemId =
        All.ToDictionary(item => item.ItemId);

    /// <summary>The per-unit copper price for an item if a coin-priced vendor sells it; otherwise null.</summary>
    public static int? CopperPriceFor(int itemId) =>
        ByItemId.TryGetValue(itemId, out VendorItem? item) ? item.CopperPerUnit : null;

    private static VendorItem Coin(int itemId, int copper, int quantity = 1, string? npc = null) =>
        new()
        {
            ItemId = itemId,
            Currency = VendorCurrency.Coin,
            Cost = copper,
            Quantity = quantity,
            Npc = npc
        };
}
