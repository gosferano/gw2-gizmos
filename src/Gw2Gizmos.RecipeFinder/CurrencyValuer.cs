using Gw2Gizmos.Data.Static.Crafting;

namespace Gw2Gizmos.RecipeFinder;

/// <summary>
/// Derives a coin (copper) value per account currency from the best <em>realizable</em> vendor → trading-post
/// arbitrage: buy a tradeable item from a vendor for the currency and resell it. Fully data-driven from the
/// vendor catalog + live trading-post prices; no manual tuning. Four guards keep it honest (a raw best-rate is
/// far too noisy — see the read-only analysis):
/// <list type="bullet">
/// <item>liquidity — only count source items with real trading-post supply, so the arbitrage is actually
/// realizable (not a thin-market luxury item);</item>
/// <item>median, not max — robust to a single mispriced/cheap offer;</item>
/// <item>account currencies only — a tradeable-item currency is valued by its own trading-post price instead,
/// and unresolved scrape-noise "currencies" (no currency id) are skipped;</item>
/// <item>minimum sample size — a currency seen on too few liquid items is left unvalued, not guessed.</item>
/// </list>
/// </summary>
public static class CurrencyValuer
{
    /// <summary>Net payout after the trading post's 15% fee — the coin you actually receive on a sale.</summary>
    private const decimal TradingPostPayout = 0.85m;

    /// <summary>Coin = currency id 1; valued 1:1, never derived.</summary>
    private const int CoinCurrencyId = 1;

    /// <inheritdoc cref="DeriveWeights(IEnumerable{VendorItem}, Func{int, ValueTuple{int, int}}, int, int)"/>
    public static IReadOnlyList<CurrencyWeight> DeriveWeights(
        Func<int, (int Sell, int Supply)> price, int minSupply = 500, int minSamples = 10) =>
        DeriveWeights(VendorItems.All, price, minSupply, minSamples);

    /// <summary>
    /// Coin value per account currency, highest first. <paramref name="price"/> returns an item's lowest sell
    /// listing (copper) and current supply; <paramref name="minSupply"/> is the liquidity gate and
    /// <paramref name="minSamples"/> the smallest number of liquid source items a currency needs to be valued.
    /// </summary>
    public static IReadOnlyList<CurrencyWeight> DeriveWeights(
        IEnumerable<VendorItem> vendorItems,
        Func<int, (int Sell, int Supply)> price,
        int minSupply = 500,
        int minSamples = 10)
    {
        var samples = new Dictionary<string, List<decimal>>(StringComparer.Ordinal);

        foreach (VendorItem item in vendorItems)
        {
            if (item.GameId is not { } itemId)
            {
                continue;
            }

            (int sell, int supply) = price(itemId);
            if (sell <= 0 || supply < minSupply)
            {
                continue; // untraded or too thin to actually liquidate the currency into
            }

            decimal liquidation = sell * TradingPostPayout;

            foreach (VendorOffer offer in item.Offers)
            {
                // Clean single-currency offers paid in an account currency. Skip Coin (valued 1:1) and item-
                // currencies — a cost is item-currency only when it has an item id but NO currency id (ecto, …);
                // several account currencies also have a same-named item, so they carry both ids and must stay.
                // Many account currencies have an unresolved currency id in the scrape, so we don't require one —
                // the supply + sample gates filter the noise "currencies" instead. Multi-component costs are out.
                if (offer.Quantity <= 0 || offer.Cost is not [{ } cost] || cost.Value <= 0
                    || cost.CurrencyId == CoinCurrencyId
                    || string.Equals(cost.Currency, "Coin", StringComparison.Ordinal)
                    || (cost.ItemId is > 0 && cost.CurrencyId is null))
                {
                    continue;
                }

                if (!samples.TryGetValue(cost.Currency, out List<decimal>? rates))
                {
                    samples[cost.Currency] = rates = [];
                }

                rates.Add(liquidation * offer.Quantity / cost.Value);
            }
        }

        return samples
            .Where(entry => entry.Value.Count >= minSamples)
            .Select(entry => new CurrencyWeight(entry.Key, Median(entry.Value), entry.Value.Count))
            .OrderByDescending(weight => weight.CopperPerUnit)
            .ToList();
    }

    private static decimal Median(List<decimal> values)
    {
        values.Sort();
        int n = values.Count;
        return n % 2 == 1 ? values[n / 2] : (values[n / 2 - 1] + values[n / 2]) / 2m;
    }
}

/// <summary>A currency's derived coin value (the median realizable arbitrage rate) and how many liquid source
/// items backed it.</summary>
public sealed record CurrencyWeight(string Currency, decimal CopperPerUnit, int Samples);
