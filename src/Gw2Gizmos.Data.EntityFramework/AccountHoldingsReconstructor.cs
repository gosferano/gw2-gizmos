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
}
