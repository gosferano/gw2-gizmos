using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Append-on-change log of a material-storage item's count for an account. Latest row per (account, item)
/// is the current count; history enables future deltas. <see cref="Category"/> is the GW2 material category.
/// </summary>
[Index(nameof(AccountId), nameof(ItemId), nameof(Id))]
public class AccountMaterialObservation
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public int ItemId { get; set; }

    public int Category { get; set; }

    public int Count { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}
