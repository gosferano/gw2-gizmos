using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// The account's estimated value at one point in time, broken down by where it's held (all in copper). The worker
/// records one point per hour after the account sync; when an account has none yet, it first backfills one point per
/// day from the holdings logs (<see cref="IsBackfilled"/>). Items are valued at their instant-sell price (see
/// <see cref="AccountValuation"/>).
/// </summary>
[Index(nameof(AccountId), nameof(TakenAtUtc))]
public class AccountValueSnapshot
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public DateTimeOffset TakenAtUtc { get; set; }

    /// <summary>Coin in the wallet.</summary>
    public long CoinCopper { get; set; }

    public long MaterialsCopper { get; set; }

    public long BankCopper { get; set; }

    public long SharedInventoryCopper { get; set; }

    /// <summary>
    /// Items in character bags, and for points recorded live, what characters have equipped too, with upgrades and
    /// infusions (legendaries from the armory aside).
    /// </summary>
    public long CharactersCopper { get; set; }

    /// <summary>
    /// Open orders: coin held in buy orders, and what listed items pay out when they sell. Only known for points
    /// recorded live — backfilled points have 0, since past orders aren't stored.
    /// </summary>
    public long TradingPostCopper { get; set; }

    /// <summary>The legendaries in the legendary armory. Only known for points recorded live, like unlocks.</summary>
    public long LegendaryArmoryCopper { get; set; }

    /// <summary>The account's unlocks: skins, dyes, minis, outfits, recipes and the rest.</summary>
    public long UnlocksCopper { get; set; }

    /// <summary>True for points reconstructed from the holdings logs rather than recorded after a sync.</summary>
    public bool IsBackfilled { get; set; }
}
