namespace Gw2Gizmos.RecipeFinder.Model;

/// <summary>
/// A single vendor offer for an item: <see cref="Quantity"/> of it for the full <see cref="Cost"/> — which can
/// be several components paid together (e.g. 25 Airship Part + 1050 Karma). Amounts are exactly as the vendor
/// charges (not divided per unit), so a "250 for 1 Guild Commendation" bundle never rounds to 0.
/// </summary>
public sealed record VendorOffer(long Quantity, IReadOnlyList<VendorCost> Cost);

/// <summary>
/// One component of a vendor offer's price: <see cref="Amount"/> of a currency. The currency is either a
/// tradeable item (<see cref="ItemId"/> set — shown via its item icon) or an account currency
/// (<see cref="IconUrl"/> set — shown via its currency icon); <see cref="Currency"/> is the name.
/// </summary>
public sealed record VendorCost(long Amount, string Currency, int? ItemId, string? IconUrl, bool IsCoin = false)
{
    /// <summary>The currency is a tradeable item (not coin) — render its icon with the item-id <c>ItemImage</c>.</summary>
    public bool HasItemIcon => !IsCoin && ItemId is > 0;

    /// <summary>The currency is an account currency (not coin) with a resolved icon — render with <c>CurrencyImage</c>.</summary>
    public bool HasCurrencyIcon => !IsCoin && !HasItemIcon && !string.IsNullOrEmpty(IconUrl);
}
