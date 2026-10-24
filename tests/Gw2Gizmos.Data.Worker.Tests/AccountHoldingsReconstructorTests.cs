using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Tests <see cref="AccountHoldingsReconstructor.ItemValues"/> — the instant-sell valuation used for session loot
/// and profit: trading-post buy order minus the 15% fee where one exists, else vendor value, except "NoSell" items
/// (which can't be vendored) are worth 0.
/// </summary>
public sealed class AccountHoldingsReconstructorTests : IDisposable
{
    private const long TradingPostTakePercent = 85;

    private readonly WorkerDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ItemValues_PricesByBuyOrderVendorAndNoSell()
    {
        using (Gw2GizmosDbContext seed = _fixture.NewContext())
        {
            // 1: vendorable, no TP price → vendor value.
            seed.Items.Add(Item(1, vendorValue: 100));
            // 2: NoSell, no TP price → 0 (can't be vendored).
            seed.Items.Add(Item(2, vendorValue: 50, noSell: true));
            // 3: vendorable + TP buy → buy minus fee (wins over vendor).
            seed.Items.Add(Item(3, vendorValue: 100));
            seed.PriceSnapshots.Add(Price(3, buy: 1000));
            // 4: NoSell BUT has a TP buy → still priced from the buy (NoSell only blocks the vendor fallback).
            seed.Items.Add(Item(4, vendorValue: 80, noSell: true));
            seed.PriceSnapshots.Add(Price(4, buy: 1000));
            // 5: vendorable; a buy of 0 isn't a real order → vendor value.
            seed.Items.Add(Item(5, vendorValue: 30));
            seed.PriceSnapshots.Add(Price(5, buy: 0));
            // 6: two snapshots — the latest (highest Id) buy is used.
            seed.Items.Add(Item(6, vendorValue: 10));
            seed.PriceSnapshots.Add(Price(6, buy: 1000));
            seed.PriceSnapshots.Add(Price(6, buy: 200));
            seed.SaveChanges();
        }

        using Gw2GizmosDbContext db = _fixture.NewContext();
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, new[] { 1, 2, 3, 4, 5, 6, 99 });

        Assert.Equal(100, values[1]);                              // vendor
        Assert.Equal(0, values[2]);                                // NoSell, no price
        Assert.Equal(1000 * TradingPostTakePercent / 100, values[3]); // 850, buy − fee
        Assert.Equal(1000 * TradingPostTakePercent / 100, values[4]); // 850, buy wins despite NoSell
        Assert.Equal(30, values[5]);                               // buy 0 ignored → vendor
        Assert.Equal(200 * TradingPostTakePercent / 100, values[6]);  // latest buy − fee
        Assert.False(values.ContainsKey(99));                      // unknown item → absent (treated as 0)
    }

    [Fact]
    public void ItemValues_WithCoinCosts_ValuesItemsWithNoBuyOrderAtTheirCoinCost()
    {
        using (Gw2GizmosDbContext seed = _fixture.NewContext())
        {
            // 1: NoSell, no TP price, made for 640c of coin → 640.
            seed.Items.Add(Item(1, vendorValue: 0, noSell: true));
            seed.ItemCoinCosts.Add(new ItemCoinCost { ItemId = 1, Cost = 639.6 });
            // 2: a TP buy wins over a coin cost (it's what it sells for).
            seed.Items.Add(Item(2, vendorValue: 0));
            seed.PriceSnapshots.Add(Price(2, buy: 1000));
            seed.ItemCoinCosts.Add(new ItemCoinCost { ItemId = 2, Cost = 5000 });
            // 3: no coin cost → vendor value, as before.
            seed.Items.Add(Item(3, vendorValue: 30));
            seed.SaveChanges();
        }

        using Gw2GizmosDbContext db = _fixture.NewContext();
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, new[] { 1, 2, 3 }, coinCosts: true);
        Dictionary<int, long> forSale = AccountHoldingsReconstructor.ItemValues(db, new[] { 1, 2, 3 });

        Assert.Equal([640, 850, 30], new[] { values[1], values[2], values[3] });
        Assert.Equal(0, forSale[1]); // without coin costs, what it sells for: nothing
    }

    [Fact]
    public void ItemValues_EmptyInput_ReturnsEmpty()
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        Assert.Empty(AccountHoldingsReconstructor.ItemValues(db, System.Array.Empty<int>()));
    }

    private static Item Item(int id, int vendorValue, bool noSell = false) =>
        new()
        {
            Id = id,
            ChatLink = "",
            Name = $"Item {id}",
            Type = "Trophy",
            Rarity = "Basic",
            VendorValue = vendorValue,
            Flags = noSell ? new List<ItemFlag> { new() { Value = "NoSell" } } : new List<ItemFlag>(),
        };

    private static PriceSnapshot Price(int itemId, int buy) =>
        new() { ItemId = itemId, TimestampUtc = DateTimeOffset.UnixEpoch, Buy = buy, Sell = buy + 1 };
}
