using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// The current slot-by-slot layout of a slot-based store (bank or shared inventory) for an account, replaced
/// wholesale each sync. One row per slot including empties (<see cref="ItemId"/> null), so the desktop can
/// draw the grid exactly as in-game. Item cosmetics (skin/dyes/upgrades) are deferred — additive later.
/// </summary>
[PrimaryKey(nameof(AccountId), nameof(Store), nameof(SlotIndex))]
public class AccountContainerSlot
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    [MaxLength(32)]
    public string Store { get; set; } = "";

    public int SlotIndex { get; set; }

    /// <summary>The item in this slot, or <c>null</c> when the slot is empty.</summary>
    public int? ItemId { get; set; }

    public int Count { get; set; }

    /// <summary>Remaining charges for consumable stacks, when applicable.</summary>
    public int? Charges { get; set; }
}
