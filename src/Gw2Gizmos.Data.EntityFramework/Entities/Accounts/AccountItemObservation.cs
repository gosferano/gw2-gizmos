using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// Append-on-change log of how many of an item an account holds in a given location (material storage, bank,
/// shared inventory, or — summed across all characters — character bags). The latest row per
/// (account, container, item) is the current count there; the history is the event-sourced series for
/// "acquired/spent over this period" deltas. Materials are just items in a location, so they live here too
/// (under <see cref="AccountContainer.MaterialStorage"/>) rather than in a parallel table; currencies are not
/// items and stay in <see cref="AccountWalletObservation"/>.
/// <para>
/// Count-only by design — per-instance properties (skin, dyes, upgrades, stats) belong on the slot snapshots
/// (<see cref="AccountContainerSlot"/> / <see cref="CharacterItemSlot"/>), not in a per-item count series.
/// </para>
/// </summary>
[Index(nameof(AccountId), nameof(Container), nameof(ItemId), nameof(Id))]
public class AccountItemObservation
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    [MaxLength(32)]
    public string Container { get; set; } = "";

    public int ItemId { get; set; }

    public int Count { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}
