using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

/// <summary>
/// An item that unlocks a novelty, outfit, finisher, mail carrier, glider or jade bot, from the unlock's catalog
/// (<c>/v2/novelties</c>' <c>unlock_item</c>, <c>/v2/outfits</c>' <c>unlock_items</c>, …), to value the account's
/// unlocks by. Dyes, minis and skins are found from the item catalog instead. Each kind's rows are replaced when its
/// catalog is fetched.
/// </summary>
[Table("UnlockItems")]
[PrimaryKey(nameof(Kind), nameof(UnlockId), nameof(ItemId))]
public class UnlockItem
{
    /// <summary>One of <see cref="AccountUnlockKind"/>.</summary>
    [MaxLength(16)]
    public string Kind { get; set; } = "";

    public int UnlockId { get; set; }

    public int ItemId { get; set; }
}
