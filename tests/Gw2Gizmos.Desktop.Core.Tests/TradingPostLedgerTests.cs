using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// Realized profit from the account's trades: sales matched to earlier purchases of the same item, oldest first,
/// after the 5% listing and 10% exchange fees; units sold without a matching purchase count as income.
/// </summary>
public sealed class TradingPostLedgerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly List<TradingPostTradeRow> _trades = [];

    private void Buy(int day, int itemId, int price, int quantity) =>
        _trades.Add(new TradingPostTradeRow(_trades.Count + 1, false, itemId, $"Item {itemId}", price, quantity,
            Start.AddDays(day)));

    private void Sell(int day, int itemId, int price, int quantity) =>
        _trades.Add(new TradingPostTradeRow(_trades.Count + 1, true, itemId, $"Item {itemId}", price, quantity,
            Start.AddDays(day)));

    [Theory]
    [InlineData(100, 85)] // 5c listing + 10c exchange
    [InlineData(10_000, 8_500)]
    [InlineData(5, 3)] // each fee at least 1c
    [InlineData(1, -1)] // a 1c sale pays out nothing, and the fees still cost 2c
    public void A_unit_pays_its_price_less_the_listing_and_exchange_fees(int price, long proceeds)
    {
        Assert.Equal(proceeds, TradingPostLedger.UnitProceeds(price));
    }

    [Fact]
    public void A_sale_is_matched_to_the_oldest_purchases_first()
    {
        Buy(0, 1, price: 100, quantity: 5);
        Buy(1, 1, price: 200, quantity: 5);
        Sell(2, 1, price: 1000, quantity: 7);

        TradingPostSale sale = Assert.Single(TradingPostLedger.Sales(_trades));

        Assert.Equal(7, sale.MatchedQuantity);
        Assert.Equal(5 * 100 + 2 * 200, sale.Cost);
        Assert.Equal(7 * 850 - 900, sale.Profit);
        Assert.Equal(0, sale.UnmatchedProceeds);
        Assert.Equal(7 * 150, sale.Fees);
    }

    [Fact]
    public void A_purchase_partly_used_by_one_sale_covers_the_next()
    {
        Buy(0, 1, price: 100, quantity: 10);
        Sell(1, 1, price: 1000, quantity: 4);
        Sell(2, 1, price: 1000, quantity: 4);

        IReadOnlyList<TradingPostSale> sales = TradingPostLedger.Sales(_trades); // newest first

        Assert.Equal([400, 400], sales.Select(s => s.Cost));
    }

    [Fact]
    public void Units_without_an_earlier_purchase_are_income_not_profit()
    {
        Sell(0, 1, price: 1000, quantity: 3); // dropped, never bought
        Buy(1, 1, price: 100, quantity: 2); // bought after: can't have been what was sold
        Buy(2, 2, price: 100, quantity: 5); // another item
        Sell(3, 1, price: 1000, quantity: 4);

        IReadOnlyList<TradingPostSale> sales = TradingPostLedger.Sales(_trades);
        TradingPostSale first = sales[1];
        TradingPostSale second = sales[0];

        Assert.Null(first.Profit);
        Assert.Equal(3 * 850, first.UnmatchedProceeds);
        Assert.Equal((2, 200L), (second.MatchedQuantity, second.Cost));
        Assert.Equal(2 * 850 - 200, second.Profit);
        Assert.Equal(2 * 850, second.UnmatchedProceeds);
    }

    [Fact]
    public void A_period_totals_its_sales_by_item_most_profitable_first()
    {
        Buy(0, 1, price: 100, quantity: 10);
        Buy(0, 2, price: 100, quantity: 10);
        Sell(1, 1, price: 1000, quantity: 1); // before the period
        Sell(5, 1, price: 1000, quantity: 2);
        Sell(6, 2, price: 2000, quantity: 1);
        Sell(6, 3, price: 500, quantity: 2); // never bought: income only

        TradingPostPeriod period = TradingPostLedger.Period(TradingPostLedger.Sales(_trades), Start.AddDays(4));

        Assert.Equal([2, 1, 3], period.Items.Select(i => i.ItemId));
        Assert.Equal(2 * (850 - 100) + (1700 - 100), period.Profit);
        Assert.Equal(2 * TradingPostLedger.UnitProceeds(500), period.Income);
        Assert.Equal(2, period.Best!.ItemId);
        TradingPostItemProfit item1 = period.Items.Single(i => i.ItemId == 1);
        Assert.Equal((2, 200L, 1500L), (item1.Sold, item1.Cost, item1.Profit));
        Assert.Equal(7.5, item1.Margin);
    }
}
