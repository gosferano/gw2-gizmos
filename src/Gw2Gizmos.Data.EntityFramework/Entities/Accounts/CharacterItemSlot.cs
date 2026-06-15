using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// The current slot-by-slot layout of one character's bags for an account, replaced wholesale each sync (one row
/// per slot including empties, so the desktop can draw the grid exactly as in-game). The per-character equivalent
/// of <see cref="AccountContainerSlot"/>, plus a <see cref="CharacterName"/> dimension. Per-item account totals
/// (summed across all characters) live in <see cref="AccountItemObservation"/> under
/// <see cref="AccountContainer.CharacterInventory"/>. Item cosmetics (skin/dyes/upgrades) are deferred — a JSON
/// column here, additive later.
/// </summary>
[PrimaryKey(nameof(CharacterName), nameof(SlotIndex))]
public class CharacterItemSlot
{
    /// <summary>The owning character — globally unique, so (name, slot) is the key; AccountId is a plain column.</summary>
    [MaxLength(64)]
    public string CharacterName { get; set; } = "";

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary>Flat slot index across the character's bags, in bag-then-slot order.</summary>
    public int SlotIndex { get; set; }

    /// <summary>The item in this slot, or <c>null</c> when the slot is empty (or the bag slot itself is empty).</summary>
    public int? ItemId { get; set; }

    public int Count { get; set; }

    /// <summary>Remaining charges for consumable stacks, when applicable.</summary>
    public int? Charges { get; set; }
}
