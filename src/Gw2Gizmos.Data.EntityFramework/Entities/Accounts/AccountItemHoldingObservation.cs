using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Append-on-change log of how many of an item an account holds in a slot-based store (bank or shared
/// inventory), summed across slots. The slot layout lives in <see cref="AccountContainerSlot"/> for the
/// current grid; this per-item total is the event-sourced series that enables future "gained X" deltas.
/// </summary>
[Index(nameof(AccountId), nameof(Store), nameof(ItemId), nameof(Id))]
public class AccountItemHoldingObservation
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public AccountContainer Store { get; set; }

    public int ItemId { get; set; }

    public int Count { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}
