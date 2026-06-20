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

        CurrencyWeight tokens = Assert.Single(weights); // only "Tokens" clears the sample gate
        Assert.Equal("Tokens", tokens.Currency);
        Assert.Equal(12, tokens.Samples);              // 11 normal + 1 outlier; the low-supply one didn't count
        Assert.Equal(85m, tokens.CopperPerUnit);       // median, unmoved by the 85,000 outlier
    }
}
