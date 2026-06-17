using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Reconstructs an account's holdings at a point in time from the append-on-change observation logs the worker
/// writes (<see cref="AccountItemObservation"/> / <see cref="AccountWalletObservation"/>). "As of time T" = the
/// latest observation per key with <c>ObservedAtUtc &lt;= T</c>. Pure query logic shared by the desktop's readers
/// and the worker's tests; runs server-side now that dates are stored as ticks (SQLite can compare/aggregate them).
/// </summary>
public static class AccountHoldingsReconstructor
{
    /// <summary>Per-item totals across every container as of <paramref name="asOfUtc"/>: the latest observation per
    /// (container, item) at or before the instant, summed per item. Items at zero are excluded.</summary>
    public static Dictionary<int, int> ItemTotalsAsOf(Gw2GizmosDbContext db, string accountId, DateTimeOffset asOfUtc)
    {
        IQueryable<long> maxIds = db.AccountItemObservations
            .Where(o => o.AccountId == accountId && o.ObservedAtUtc <= asOfUtc)
            .GroupBy(o => new { o.Container, o.ItemId })
            .Select(g => g.Max(x => x.Id));

        var totals = new Dictionary<int, int>();
        foreach (var row in db.AccountItemObservations.AsNoTracking()
                     .Where(o => maxIds.Contains(o.Id) && o.Count > 0)
                     .Select(o => new { o.ItemId, o.Count }))
        {
            totals[row.ItemId] = totals.GetValueOrDefault(row.ItemId) + row.Count;
        }

        return totals;
    }

    /// <summary>The wallet balance per currency as of <paramref name="asOfUtc"/> (latest observation at or before).</summary>
    public static Dictionary<int, long> WalletTotalsAsOf(Gw2GizmosDbContext db, string accountId, DateTimeOffset asOfUtc)
    {
        IQueryable<long> maxIds = db.AccountWalletObservations
            .Where(o => o.AccountId == accountId && o.ObservedAtUtc <= asOfUtc)
            .GroupBy(o => o.CurrencyId)
            .Select(g => g.Max(x => x.Id));

        return db.AccountWalletObservations.AsNoTracking()
            .Where(o => maxIds.Contains(o.Id))
            .ToDictionary(o => o.CurrencyId, o => o.Value);
    }

    /// <summary>The balance of a single currency as of <paramref name="asOfUtc"/>, or 0 if never observed.</summary>
    public static long WalletValueAsOf(Gw2GizmosDbContext db, string accountId, int currencyId, DateTimeOffset asOfUtc) =>
        db.AccountWalletObservations.AsNoTracking()
            .Where(o => o.AccountId == accountId && o.CurrencyId == currencyId && o.ObservedAtUtc <= asOfUtc)
            .OrderByDescending(o => o.Id)
            .Select(o => o.Value)
            .FirstOrDefault();

    // 15% trading-post fee taken on a sale — the buy-order value is kept at this percent so loot reads as the
    // realistic take-home if sold instantly.
    private const long TradingPostTakePercent = 85;

    // GW2 item flag for items that can't be sold to a vendor — their vendor value isn't realizable.
    private const string NoSellFlag = "NoSell";

    /// <summary>
    /// Per-item "instant-sell" unit value in copper: the latest trading-post buy order minus the 15% fee where one
    /// exists, otherwise the item's vendor value — but only when it's sellable ("NoSell" items can't be vendored, so
    /// they're worth 0). Items with neither resolve to 0. Used to value session loot/profit.
    /// </summary>
    public static Dictionary<int, long> ItemValues(Gw2GizmosDbContext db, IReadOnlyCollection<int> itemIds)
    {
        var values = new Dictionary<int, long>();
        int[] distinct = itemIds.Distinct().ToArray();
        if (distinct.Length == 0)
        {
            return values;
        }

        // Batched to stay under SQLite's parameter cap.
        foreach (int[] batch in distinct.Chunk(500))
        {
            IQueryable<long> latestIds = db.PriceSnapshots
                .Where(s => batch.Contains(s.ItemId))
                .GroupBy(s => s.ItemId)
                .Select(g => g.Max(s => s.Id));
            Dictionary<int, int> buy = db.PriceSnapshots.AsNoTracking()
                .Where(s => latestIds.Contains(s.Id) && s.Buy != null && s.Buy > 0)
                .ToDictionary(s => s.ItemId, s => s.Buy!.Value);

            foreach (var item in db.Items.AsNoTracking()
                         .Where(i => batch.Contains(i.Id))
                         .Select(i => new { i.Id, i.VendorValue, NoSell = i.Flags.Any(f => f.Value == NoSellFlag) }))
            {
                values[item.Id] = buy.TryGetValue(item.Id, out int b)
                    ? b * TradingPostTakePercent / 100
                    : item.NoSell ? 0 : item.VendorValue;
            }

            // A priced item missing from the catalog (shouldn't happen) — still value it from the buy order.
            foreach ((int id, int b) in buy)
            {
                if (!values.ContainsKey(id))
                {
                    values[id] = b * TradingPostTakePercent / 100;
                }
            }
        }

        return values;
    }
}
