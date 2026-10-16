using System.Collections.Generic;
using System.Linq;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Decides which open orders to notify about: the ones beaten (undercut or outbid) that weren't already beaten last
/// time. An order is notified once when it gets beaten; if it later becomes the best again (the competitor sold out,
/// or the user relisted) and is beaten again, it's notified again.
/// </summary>
public static class TradingPostAlerts
{
    /// <summary>Above this many at once, one summary notification instead of one per order.</summary>
    public const int MaxIndividualNotifications = 3;

    /// <param name="orders">The account's open orders as of the last sync.</param>
    /// <param name="notified">Ids of the orders already notified as beaten.</param>
    /// <returns>The orders to notify about now, and the beaten set to remember for next time.</returns>
    public static (List<TradingPostOrderRow> NewlyBeaten, HashSet<long> Beaten) Check(
        IReadOnlyCollection<TradingPostOrderRow> orders,
        IReadOnlySet<long> notified)
    {
        List<TradingPostOrderRow> beaten = orders.Where(o => o.IsBeaten).ToList();
        return (beaten.Where(o => !notified.Contains(o.Id)).ToList(), beaten.Select(o => o.Id).ToHashSet());
    }

    /// <summary>The notification for one beaten order, e.g. "Listing undercut" / "Mystic Coin: listed at 1g …".</summary>
    public static (string Title, string Message) Describe(TradingPostOrderRow order)
    {
        long gap = order.GapCopper ?? 0;
        return order.IsBuy
            ? ("Buy order outbid",
                $"{order.Name}: you offer {Coin.Format(order.Price)}, someone now offers "
                + $"{Coin.Format(order.BestBuyPrice ?? 0)} ({Coin.Format(gap)} more).")
            : ("Listing undercut",
                $"{order.Name}: you list at {Coin.Format(order.Price)}, someone now sells at "
                + $"{Coin.Format(order.BestSellPrice ?? 0)} ({Coin.Format(gap)} less).");
    }

    /// <summary>The single notification for many beaten orders at once.</summary>
    public static (string Title, string Message) Summarize(IReadOnlyCollection<TradingPostOrderRow> orders) =>
        ($"{orders.Count} trading-post orders undercut or outbid",
            string.Join(", ", orders.Select(o => o.Name).Distinct()) + ".");
}
