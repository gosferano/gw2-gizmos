namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>What an NPC vendor charges in. Only <see cref="Coin"/> gives a direct copper price the engine can
/// use today; the others are recorded for completeness (and a future currency-to-coin conversion).</summary>
public enum VendorCurrency
{
    Coin,
    Karma,
    SpiritShard,
    Gem,
    DungeonToken,
    Other
}

/// <summary>
/// A fixed price an NPC vendor sells an item for — data the GW2 API doesn't expose (it only has the
/// merchant <em>sell-back</em> value). Used as a price floor/fallback when an item has no trading-post
/// listing, so vendor-bought mats (Thermocatalytic Reagent, thread, scraps, …) aren't valued at 0.
/// </summary>
public sealed record VendorItem
{
    public required int ItemId { get; init; }

    public required VendorCurrency Currency { get; init; }

    /// <summary>Price charged for one purchase (in the unit of <see cref="Currency"/>).</summary>
    public required int Cost { get; init; }

    /// <summary>How many items a single purchase yields (most are 1).</summary>
    public int Quantity { get; init; } = 1;

    /// <summary>Vendor name / location, informational (e.g. "Master Craftsman, Lion's Arch").</summary>
    public string? Npc { get; init; }

    /// <summary>Per-item cost in copper, or null when priced in a non-coin currency we don't convert yet.</summary>
    public int? CopperPerUnit => Currency == VendorCurrency.Coin ? Cost / Quantity : null;
}
