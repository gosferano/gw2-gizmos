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
/// minus the trading-post fee), or for an item the trading post doesn't buy, what it costs for coin (made or bought
/// from a vendor, e.g. a Mystic Clover), else its vendor price, else 0; plus its open trading-post orders, what
/// characters have equipped, its legendary armory and its unlocks (<see cref="AccountUnlockValuation"/>), valued the
/// same way. Currencies other than coin aren't counted.
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
            db, held.Select(h => h.ItemId).ToList(), historicalPrices ? asOfUtc : null, coinCosts: true);

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
    /// What the account's characters have equipped, with upgrades and infusions, as of the last sync. Legendaries
    /// equipped from the legendary armory are counted with the armory (<see cref="LegendaryArmoryValue"/>), but what's
    /// slotted in them is counted here.
    /// </summary>
    public static long EquipmentValue(Gw2GizmosDbContext db, string accountId)
    {
        List<int> equipped = db.CharacterEquippedItems.AsNoTracking()
            .Where(e => e.AccountId == accountId
                && !(e.Part == EquippedPart.Item && e.Location.Contains("LegendaryArmory")))
            .Select(e => e.ItemId)
            .ToList();
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(db, equipped, coinCosts: true);
        return equipped.Sum(itemId => values.GetValueOrDefault(itemId));
    }

    /// <summary>The legendaries in the account's legendary armory, each as many times as it holds them.</summary>
    public static long LegendaryArmoryValue(Gw2GizmosDbContext db, string accountId)
    {
        var armory = db.AccountLegendaryArmory.AsNoTracking()
            .Where(l => l.AccountId == accountId)
            .Select(l => new { l.ItemId, l.Count })
            .ToList();
        Dictionary<int, long> values = AccountHoldingsReconstructor.ItemValues(
            db, armory.Select(l => l.ItemId).ToList(), coinCosts: true);
        return armory.Sum(l => l.Count * values.GetValueOrDefault(l.ItemId));
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

/// <summary>
/// An account's estimated value by where it's held, in copper. What characters have equipped (part of
/// <see cref="Characters"/>), the legendary armory and unlocks are only known as of the last sync, so they're left out
/// of past points.
/// </summary>
public sealed record AccountValue(
    long Coin, long Materials, long Bank, long SharedInventory, long Characters, long TradingPost,
    long LegendaryArmory = 0, long Unlocks = 0)
{
    public long Total =>
        Coin + Materials + Bank + SharedInventory + Characters + TradingPost + LegendaryArmory + Unlocks;
}
