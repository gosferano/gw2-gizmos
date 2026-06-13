using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Materials;

/// <summary>
/// Membership of an item in a <see cref="MaterialCategory"/>, in its in-game slot order. Master data from
/// <c>/v2/materials</c>; the account's per-item count comes from the account material observations.
/// </summary>
[PrimaryKey(nameof(CategoryId), nameof(ItemId))]
public class MaterialCategoryItem
{
    public int CategoryId { get; set; }

    public int ItemId { get; set; }

    /// <summary>Position of the item within its category (the order returned by the API).</summary>
    public int Position { get; set; }
}
