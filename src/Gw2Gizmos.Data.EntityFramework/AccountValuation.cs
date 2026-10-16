using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework;

/// <summary>
/// Estimates what an account is worth: its coin plus every item it holds (material storage, bank, shared inventory,
/// character bags) at its instant-sell value (<see cref="AccountHoldingsReconstructor.ItemValues"/>: the buy order
/// minus the trading-post fee, else the vendor price, else 0), plus its open trading-post orders. Equipment and
/// other currencies aren't counted.
/// </summary>
public static class AccountValuation
{
    /// <summary>The wallet currency id of coin.</summary>
    public const int CoinCurrencyId = 1;

    // A listing's 5% fee is paid upfront, so a sell order still pays out its price minus the 10% exchange fee.
    private const long ListingPayoutPercent = 90;

    /// <summary>
    /// The account's holdings as of <paramref name="asOfUtc"/>, valued at that time's prices where the price history
    /// has them (<paramref name="historicalPrices"/>) or today's. Trading-post orders aren't included (they aren't
    /// kept historically); see <see cref="TradingPostValue"/>.
    /// </summary>
    public static AccountValue HoldingsAsOf(
        Gw2GizmosDbContext db, string accountId, DateTimeOffset asOfUtc, bool historicalPrices)
    {
        IQueryable<long> maxIds = db.AccountItemObservations
            .Where(o => o.AccountId == accountId && o.ObservedAtUtc <= asOfUtc)
            .GroupBy(o => new { o.Container, o.ItemId })
            .Select(g => g.Max(x => x.Id));
        var held = db.AccountItemObservations.AsNoTracking()
            .Where(o => maxIds.Contains(o.Id) && o.Count > 0)
            .Select(o => new { o.Container, o.ItemId, o.Count })
            .ToList();

        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(
            db, held.Select(h => h.ItemId).ToList(), historicalPrices ? asOfUtc : null);

        long Sum(string container) => held
            .Where(h => h.Container == container)
            .Sum(h => h.Count * values.GetValueOrDefault(h.ItemId));

        return new AccountValue(
            Coin: AccountHoldingsReconstructor.WalletValueAsOf(db, accountId, CoinCurrencyId, asOfUtc),
            Materials: Sum(AccountContainer.MaterialStorage),
            Bank: Sum(AccountContainer.Bank),
            SharedInventory: Sum(AccountContainer.SharedInventory),
            Characters: Sum(AccountContainer.CharacterInventory),
            TradingPost: 0);
    }

    /// <summary>
    /// The account's open trading-post orders as of the last sync: the coin held in its buy orders, plus what its
    /// listings pay out when they sell.
    /// </summary>
    public static long TradingPostValue(Gw2GizmosDbContext db, string accountId) =>
        db.TradingPostOrders.AsNoTracking()
            .Where(o => o.AccountId == accountId)
            .Select(o => new { o.Side, o.Price, o.Quantity })
            .AsEnumerable()
            .Sum(o => o.Side == TradingPostSide.Buy
                ? (long)o.Price * o.Quantity
                : (long)o.Price * o.Quantity * ListingPayoutPercent / 100);
}

/// <summary>An account's estimated value by where it's held, in copper.</summary>
public sealed record AccountValue(
    long Coin, long Materials, long Bank, long SharedInventory, long Characters, long TradingPost)
{
    public long Total => Coin + Materials + Bank + SharedInventory + Characters + TradingPost;
}
