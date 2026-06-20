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
/// Two hand-maintained tables then correct what the market can't show: <see cref="ScarcityMultipliers"/> raises
/// effort-gated currencies the arbitrage floor undervalues, and <see cref="ExplicitValues"/> hard-sets currencies
/// the floor gets wrong (farmable ones the sampler can't reach; karma, which the floor overstates).
/// </summary>
public static class CurrencyValuer
{
    /// <summary>Net payout after the trading post's 15% fee — the coin you actually receive on a sale.</summary>
    private const decimal TradingPostPayout = 0.85m;

    /// <summary>Coin = currency id 1; valued 1:1, never derived.</summary>
    private const int CoinCurrencyId = 1;

    /// <summary>
    /// Per-currency scarcity multipliers applied on top of the arbitrage floor. The floor (best vendor → TP
    /// resale) is a fair value for freely-farmable currencies whose earning effort ≈ their resale value, but it
    /// badly undervalues effort-gated / capped currencies: a Fractal Relic resells for ~19c yet is far harder to
    /// come by than 19c of karma. Market data carries no signal for "hard to come by", so these factors are a
    /// deliberate, hand-maintained judgement. A currency not listed here keeps its raw arbitrage value (×1).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, decimal> ScarcityMultipliers =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["Guild Commendation"] = 3m, // weekly-capped behind active guild missions — very hard to amass
            ["Fractal Relic"] = 3m,
            ["Ancient Coin"] = 3m,
            ["Tale of Dungeon Delving"] = 2m,
        };

    /// <summary>
    /// Hand-set copper-per-unit values that <em>replace</em> the derived weight (and any multiplier), for the two
    /// cases the market floor gets wrong:
    /// <list type="bullet">
    /// <item>farmable currencies the arbitrage sampler can't reach — their offers buy only account-bound rewards
    /// (gear, recipes, obsidian shards), so there is no vendor → TP resale to measure; without a value a route
    /// paying in (say) Volatile Magic would be unpriceable and never win;</item>
    /// <item>Karma — its liquidation floor (~0.17c) overstates its real worth to a player who hoards millions and
    /// never spends it otherwise, so coin-bearing routes wrongly beat pure-karma ones; valued near-free here.</item>
    /// </list>
    /// Currencies absent both here and from the derived weights stay unvalued on purpose — prestige, capped or
    /// cosmetic currencies that should sort last.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, decimal> ExplicitValues =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["Karma"] = 0.04m, // near-free: hoarded by the million, rarely the limiting cost
            ["Spirit Shard"] = 40m,
            ["Geode"] = 0.5m,
            ["Trade Contract"] = 0.5m,
            ["Ley Line Crystal"] = 0.5m,
            ["Lump of Aurillium"] = 0.5m,
            ["Airship Part"] = 0.5m,
            ["Bandit Crest"] = 0.5m,
            ["War Supplies"] = 0.5m,
            ["Volatile Magic"] = 0.2m,
            ["Unbound Magic"] = 0.15m,
        };

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

        var derived = samples
            .Where(entry => entry.Value.Count >= minSamples)
            .ToDictionary(entry => entry.Key, entry => Median(entry.Value), StringComparer.Ordinal);

        // Union the data-derived currencies with the explicit-value list so a farmable currency the arbitrage
        // can't reach still gets a value. An explicit value wins outright; otherwise derived × scarcity multiplier.
        return derived.Keys.Union(ExplicitValues.Keys, StringComparer.Ordinal)
            .Select(name =>
            {
                decimal value = ExplicitValues.TryGetValue(name, out decimal fixedValue)
                    ? fixedValue
                    : derived.GetValueOrDefault(name) * ScarcityMultipliers.GetValueOrDefault(name, 1m);
                return new CurrencyWeight(name, value, samples.TryGetValue(name, out List<decimal>? rates) ? rates.Count : 0);
            })
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
