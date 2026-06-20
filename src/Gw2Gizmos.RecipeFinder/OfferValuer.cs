using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.RecipeFinder;

/// <summary>
/// Converts a vendor offer's full cost to a single coin (copper) figure so offers can be ranked against each
/// other (and, later, against the trading post and crafting). Each component is valued by its kind: coin at
/// 1:1, a tradeable-item currency by its own market price, an account currency by the derived weight from
/// <see cref="CurrencyValuer"/>. If any component is an account currency we couldn't value, the whole offer is
/// unpriceable (<c>null</c>) — it stays obtainable but never wins the "cheapest" comparison.
/// </summary>
public static class OfferValuer
{
    /// <param name="currencyWeights">Account currency name → copper per unit, from <see cref="CurrencyValuer"/>.</param>
    /// <param name="itemValue">A tradeable-item currency's coin value per unit (e.g. its trading-post price).</param>
    public static decimal? CoinValue(
        VendorOffer offer, IReadOnlyDictionary<string, decimal> currencyWeights, Func<int, int> itemValue)
    {
        decimal total = 0m;
        foreach (VendorCost cost in offer.Cost)
        {
            if (cost.IsCoin)
            {
                total += cost.Amount;
            }
            else if (cost.ItemId is > 0 and { } itemId)
            {
                total += cost.Amount * itemValue(itemId);
            }
            else if (currencyWeights.TryGetValue(cost.Currency, out decimal weight))
            {
                total += cost.Amount * weight;
            }
            else
            {
                return null; // an account currency with no derived weight → can't price this offer
            }
        }

        return total;
    }
}
