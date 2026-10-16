namespace Gw2Gizmos.Desktop.Core.Tests;

public sealed class TradingPostAlertsTests
{
    private static TradingPostOrderRow Sell(long id, int price, int? bestSell) =>
        new(id, IsBuy: false, ItemId: 19976, Name: "Mystic Coin", price, Quantity: 10,
            DateTimeOffset.UtcNow, BestBuyPrice: null, BestSellPrice: bestSell);

    private static TradingPostOrderRow Buy(long id, int price, int? bestBuy) =>
        new(id, IsBuy: true, ItemId: 19976, Name: "Mystic Coin", price, Quantity: 10,
            DateTimeOffset.UtcNow, BestBuyPrice: bestBuy, BestSellPrice: null);

    [Fact]
    public void Check_NotifiesOnlyOrdersNewlyBeaten()
    {
        TradingPostOrderRow[] orders =
        {
            Sell(1, price: 100, bestSell: 90), // undercut, already notified
            Sell(2, price: 100, bestSell: 95), // undercut, new
            Buy(3, price: 100, bestBuy: 110), // outbid, new
            Sell(4, price: 100, bestSell: 100), // the lowest listing (its own)
            Buy(5, price: 100, bestBuy: 100), // the highest buy order
        };

        (List<TradingPostOrderRow> newlyBeaten, HashSet<long> beaten) =
            TradingPostAlerts.Check(orders, new HashSet<long> { 1 });

        Assert.Equal(new long[] { 2, 3 }, newlyBeaten.Select(o => o.Id));
        Assert.Equal(new long[] { 1, 2, 3 }, beaten.Order());
    }

    [Fact]
    public void Check_ForgetsOrdersNoLongerBeaten_SoTheyNotifyAgain()
    {
        // Order 1 recovered (the cheaper listing sold out), so it leaves the beaten set...
        (_, HashSet<long> beaten) = TradingPostAlerts.Check(new[] { Sell(1, 100, 100) }, new HashSet<long> { 1 });
        Assert.Empty(beaten);

        // ...and a later undercut notifies again.
        (List<TradingPostOrderRow> newlyBeaten, _) = TradingPostAlerts.Check(new[] { Sell(1, 100, 80) }, beaten);
        Assert.Single(newlyBeaten);
    }

    [Fact]
    public void Describe_StatesThePricesAndGap()
    {
        (string title, string message) = TradingPostAlerts.Describe(Sell(1, price: 120, bestSell: 115));

        Assert.Equal("Listing undercut", title);
        Assert.Contains("Mystic Coin", message);
        Assert.Contains(Coin.Format(115), message);
        Assert.Contains($"({Coin.Format(5)} less)", message);
        Assert.Equal("Buy order outbid", TradingPostAlerts.Describe(Buy(1, 100, 110)).Title);
    }
}
