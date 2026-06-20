namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>
/// An item sold by one or more NPC vendors — data the GW2 API doesn't expose (it only has the merchant
/// sell-back value). Scraped from the wiki (see <c>tools/Gw2Gizmos.Wiki.DataScraper</c>) and embedded whole,
/// so the full price (every vendor, every currency) is available — not just coin. The recipe engine uses the
/// derived <see cref="CopperPerUnit"/> as a price floor; the UI can show the complete offer list.
/// </summary>
public sealed record VendorItem
{
    /// <summary>The sold item's GW2 item id (null for the rare wiki page with no game id).</summary>
    public int? GameId { get; init; }

    /// <summary>The item's wiki/display name.</summary>
    public string Item { get; init; } = "";

    /// <summary>Every vendor offer for this item (different vendors, currencies, quantities).</summary>
    public IReadOnlyList<VendorOffer> Offers { get; init; } = [];

    /// <summary>
    /// Cheapest coin (copper) price per single unit across all offers, or null if no vendor sells it for plain
    /// coin. This is what the recipe engine uses today; currency-priced offers await a currency→coin model.
    /// </summary>
    public int? CopperPerUnit
    {
        get
        {
            int? best = null;
            foreach (VendorOffer offer in Offers)
            {
                if (offer.Quantity > 0 && offer.Cost is [{ Currency: "Coin", Value: > 0 } coin])
                {
                    int perUnit = coin.Value / offer.Quantity;
                    best = best is null ? perUnit : Math.Min(best.Value, perUnit);
                }
            }

            return best;
        }
    }
}

/// <summary>One vendor's offer for an item: a vendor, how many you get, and what it costs (possibly several
/// components, e.g. coin + a currency).</summary>
public sealed record VendorOffer
{
    public string Vendor { get; init; } = "";

    public int Quantity { get; init; } = 1;

    public IReadOnlyList<CostComponent> Cost { get; init; } = [];
}

/// <summary>One component of a price: <see cref="Value"/> of <see cref="Currency"/>. The currency is either a
/// tradeable item (<see cref="ItemId"/> set) or an account currency (<see cref="CurrencyId"/> set; Coin = 1).</summary>
public sealed record CostComponent
{
    public int Value { get; init; }

    public string Currency { get; init; } = "";

    /// <summary>/v2/items id when the currency paid is itself a tradeable item (ecto, tokens, …).</summary>
    public int? ItemId { get; init; }

    /// <summary>/v2/currencies id when the currency is an account currency (Coin, Karma, Volatile Magic, …).</summary>
    public int? CurrencyId { get; init; }
}
