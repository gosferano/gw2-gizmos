using System;
using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A completed sale and what it made: its proceeds after the trading post's fees, split between the units matched to
/// earlier purchases of the same item (whose cost is known, so they make a profit) and the rest (got some other way:
/// drops, crafts, or bought before the app recorded trades), which count as income.
/// </summary>
public sealed record TradingPostSale(
    long TradeId,
    int ItemId,
    string Name,
    DateTimeOffset SoldUtc,
    int Quantity,
    long Revenue,
    long Fees,
    int MatchedQuantity,
    long MatchedProceeds,
    long Cost,
    long UnmatchedProceeds
)
{
    /// <summary>What the sale paid, after fees.</summary>
    public long Proceeds => MatchedProceeds + UnmatchedProceeds;

    /// <summary>The matched units' proceeds less what they cost; null when no unit was matched.</summary>
    public long? Profit => MatchedQuantity > 0 ? MatchedProceeds - Cost : null;
}

/// <summary>An item's sales over a period: how many sold, for what, at what cost, and what that made.</summary>
public sealed record TradingPostItemProfit(
    int ItemId,
    string Name,
    int Sold,
    long Proceeds,
    long Cost,
    long Profit,
    long Income,
    long Fees
)
{
    /// <summary>Profit over the matched cost, e.g. 0.25 for 25%; null without a matched cost.</summary>
    public double? Margin => Cost > 0 ? (double)Profit / Cost : null;
}

/// <summary>A period's sales in total: profit on matched units, income from the rest, and the fees paid.</summary>
public sealed record TradingPostPeriod(
    long Profit, long Income, long Fees, IReadOnlyList<TradingPostItemProfit> Items)
{
    public static TradingPostPeriod Empty { get; } = new(0, 0, 0, []);

    /// <summary>The item that made the most profit; null when none made any.</summary>
    public TradingPostItemProfit? Best => Items.Where(i => i.Profit > 0).MaxBy(i => i.Profit);
}

/// <summary>
/// Realized profit from the account's own trades. Each sale is matched to earlier purchases of the same item, oldest
/// first (first in, first out), at what they cost; a sale pays its price less the 5% listing fee and 10% exchange fee,
/// each at least 1 copper a unit. The listing fee of a listing cancelled before selling isn't in the API, so it can't
/// be counted.
/// </summary>
public static class TradingPostLedger
{
    /// <summary>Every sale in <paramref name="trades"/> with what it made, newest first.</summary>
    public static IReadOnlyList<TradingPostSale> Sales(IEnumerable<TradingPostTradeRow> trades)
    {
        // Purchases not yet matched to a sale, per item, oldest first.
        var lots = new Dictionary<int, Queue<Lot>>();
        var sales = new List<TradingPostSale>();
        foreach (TradingPostTradeRow trade in trades.OrderBy(t => t.PurchasedUtc).ThenBy(t => t.Id))
        {
            if (!lots.TryGetValue(trade.ItemId, out Queue<Lot>? queue))
            {
                queue = new Queue<Lot>();
                lots[trade.ItemId] = queue;
            }

            if (!trade.IsSell)
            {
                queue.Enqueue(new Lot(trade.Quantity, trade.Price));
                continue;
            }

            // Match the sale's units to the oldest purchases first; a lot partly used stays at the front.
            int matched = 0;
            long cost = 0;
            while (matched < trade.Quantity && queue.TryPeek(out Lot? lot))
            {
                int take = Math.Min(lot.Left, trade.Quantity - matched);
                matched += take;
                cost += (long)take * lot.Price;
                lot.Left -= take;
                if (lot.Left == 0)
                {
                    queue.Dequeue();
                }
            }

            long unitProceeds = UnitProceeds(trade.Price);
            sales.Add(new TradingPostSale(
                trade.Id,
                trade.ItemId,
                trade.Name,
                trade.PurchasedUtc,
                trade.Quantity,
                (long)trade.Price * trade.Quantity,
                (long)(trade.Price - unitProceeds) * trade.Quantity,
                matched,
                unitProceeds * matched,
                cost,
                unitProceeds * (trade.Quantity - matched)));
        }

        sales.Reverse();
        return sales;
    }

    /// <summary>The sales made from <paramref name="from"/> on, by item, most profitable first.</summary>
    public static TradingPostPeriod Period(IEnumerable<TradingPostSale> sales, DateTimeOffset from)
    {
        List<TradingPostSale> inPeriod = sales.Where(s => s.SoldUtc >= from).ToList();
        if (inPeriod.Count == 0)
        {
            return TradingPostPeriod.Empty;
        }

        List<TradingPostItemProfit> items = inPeriod
            .GroupBy(s => s.ItemId)
            .Select(item => new TradingPostItemProfit(
                item.Key,
                item.First().Name,
                item.Sum(s => s.Quantity),
                item.Sum(s => s.Proceeds),
                item.Sum(s => s.Cost),
                item.Sum(s => s.Profit ?? 0),
                item.Sum(s => s.UnmatchedProceeds),
                item.Sum(s => s.Fees)))
            .OrderByDescending(i => i.Profit)
            .ThenByDescending(i => i.Income)
            .ToList();
        return new TradingPostPeriod(
            items.Sum(i => i.Profit), items.Sum(i => i.Income), items.Sum(i => i.Fees), items);
    }

    /// <summary>What a unit sold at <paramref name="price"/> pays: the price less the 5% listing and 10% exchange
    /// fees, each rounded and at least 1 copper.</summary>
    public static long UnitProceeds(int price) =>
        price - Fee(price, 0.05) - Fee(price, 0.10);

    /// <summary>Units of a purchase not yet matched to a sale, and what each cost.</summary>
    private sealed class Lot(int left, int price)
    {
        public int Left { get; set; } = left;

        public int Price { get; } = price;
    }

    private static long Fee(int price, double rate) => Math.Max(1, (long)Math.Round(price * rate));
}
