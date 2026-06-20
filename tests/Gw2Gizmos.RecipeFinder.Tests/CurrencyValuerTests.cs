using Gw2Gizmos.Data.Static.Crafting;
using Gw2Gizmos.RecipeFinder;

namespace Gw2Gizmos.RecipeFinder.Tests;

public class CurrencyValuerTests
{
    private static VendorItem Vendor(int itemId, int costAmount, string currency, int? currencyId, int? costItemId = null) =>
        new()
        {
            GameId = itemId,
            Item = $"item{itemId}",
            Offers =
            [
                new VendorOffer
                {
                    Vendor = "v",
                    Quantity = 1,
                    Cost = [new CostComponent { Value = costAmount, Currency = currency, ItemId = costItemId, CurrencyId = currencyId }],
                },
            ],
        };

    [Fact]
    public void DeriveWeights_TakesMedianOfLiquidAccountCurrencyOffers_AndSkipsNoiseAndSparseCurrencies()
    {
        var items = new List<VendorItem>();

        // 12 liquid items priced 1 token each (currency id 99). 11 sell for 100c → rate 85; one is a thin/luxury
        // outlier (id 100) → rate 85,000. The median must ignore the outlier.
        for (int i = 1; i <= 11; i++)
        {
            items.Add(Vendor(itemId: i, costAmount: 1, currency: "Tokens", currencyId: 99));
        }

        items.Add(Vendor(itemId: 100, costAmount: 1, currency: "Tokens", currencyId: 99)); // outlier price below
        items.Add(Vendor(itemId: 500, costAmount: 1, currency: "Tokens", currencyId: 99)); // low supply → ignored
        items.Add(Vendor(itemId: 200, costAmount: 1, currency: "Rare", currencyId: 50));    // only 1 sample → unvalued
        items.Add(Vendor(itemId: 300, costAmount: 1, currency: "Ecto", currencyId: null, costItemId: 19721)); // item-currency
        items.Add(Vendor(itemId: 400, costAmount: 1, currency: "Coin", currencyId: 1));     // coin

        (int Sell, int Supply) Price(int id) => id switch
        {
            100 => (100_000, 1000), // liquid, but wildly priced
            500 => (100, 100),      // priced, but below the supply gate
            _ => (100, 1000),
        };

        IReadOnlyList<CurrencyWeight> weights = CurrencyValuer.DeriveWeights(items, Price, minSupply: 500, minSamples: 10);

        CurrencyWeight tokens = Assert.Single(weights, w => w.Currency == "Tokens"); // only "Tokens" clears the sample gate
        Assert.Equal(12, tokens.Samples);              // 11 normal + 1 outlier; the low-supply one didn't count
        Assert.Equal(85m, tokens.CopperPerUnit);       // median, unmoved by the 85,000 outlier
    }

    [Fact]
    public void DeriveWeights_ValuesFarmableCurrenciesTheArbitrageCannotReach()
    {
        // No vendor offers at all → nothing derived, but the explicit value still surfaces a farmable currency
        // (its real offers buy only account-bound rewards, so it would otherwise be unpriceable and never win).
        IReadOnlyList<CurrencyWeight> weights = CurrencyValuer.DeriveWeights([], _ => (0, 0));

        CurrencyWeight volatileMagic = Assert.Single(weights, w => w.Currency == "Volatile Magic");
        Assert.Equal(0.2m, volatileMagic.CopperPerUnit);
        Assert.Equal(0, volatileMagic.Samples); // hand-set, not data-derived
    }

    [Fact]
    public void DeriveWeights_ExplicitValueOverridesTheDerivedFloor()
    {
        // Karma's liquidation floor would derive ~0.85c here, but the explicit value (near-free) must replace it,
        // not be maxed against it — otherwise coin-bearing routes wrongly beat pure-karma ones.
        var items = Enumerable.Range(1, 12).Select(i => Vendor(itemId: i, costAmount: 1, currency: "Karma", currencyId: 2)).ToList();

        IReadOnlyList<CurrencyWeight> weights = CurrencyValuer.DeriveWeights(items, _ => (100, 1000));

        CurrencyWeight karma = Assert.Single(weights, w => w.Currency == "Karma");
        Assert.Equal(0.04m, karma.CopperPerUnit); // explicit value wins outright over the 0.85c derived floor
    }
}
