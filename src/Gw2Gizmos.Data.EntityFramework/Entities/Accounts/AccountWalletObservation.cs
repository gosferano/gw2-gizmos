using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Append-on-change log of a wallet currency's balance for an account. The latest row per
/// (account, currency) is the current balance; the history enables future "acquired this session" deltas.
/// </summary>
[Index(nameof(AccountId), nameof(CurrencyId), nameof(Id))]
public class AccountWalletObservation
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public int CurrencyId { get; set; }

    public long Value { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}
