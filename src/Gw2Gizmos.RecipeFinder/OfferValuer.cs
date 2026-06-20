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
            else if (currencyWeights.TryGetValue(cost.Currency, out decimal weight))
            {
                // A valued account currency — by name, so it wins over a spurious same-named item id (several
                // account currencies, e.g. Festival Token, also have an untradeable same-named item).
                total += cost.Amount * weight;
            }
            else if (cost.CurrencyId is > 0)
            {
                return null; // an account currency we couldn't value → the whole offer is unpriceable
            }
            else if (cost.ItemId is > 0 and { } itemId)
            {
                int unit = itemValue(itemId);
                if (unit <= 0)
                {
                    // An account-bound token used as a currency (e.g. Found Belonging) — has an item id but no
                    // trading-post price, so it isn't a tradeable-item currency at all. Don't price it at 0 (that
                    // wrongly makes the whole offer "free" and win); leave the offer unpriceable so it sorts last.
                    return null;
                }

                total += cost.Amount * unit; // a tradeable-item currency (ecto, tokens, …)
            }
            else
            {
                return null; // an unresolved currency with no weight → can't price this offer
            }
        }

        return total;
    }
}
