using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

/// <summary>
/// A precomputed craft-cost cache: the cheapest fully-priced cost to craft one of an item, found by walking
/// its ingredient tree with the RecipeFinder engine. The worker rebuilds it on each commerce refresh and
/// replaces it wholesale. It exists only because that tree walk is expensive (seconds for the whole item
/// universe) and can't run per grid render — everything else the Items grid shows (live buy/sell, profit,
/// margin) is derived at read time from the price history instead. One row per craftable item whose cost is
/// fully known; absence means the item isn't craftable or its recipe can't be fully priced.
/// </summary>
[Table("ItemCraftCosts")]
public class ItemCraftCost
{
    /// <summary>The GW2 item id; the natural key (one row per craftable item, not database-generated).</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }

    /// <summary>Cheapest cost to craft one (copper), walking the ingredient tree. Stored as <c>double</c>
    /// (SQLite REAL) so the grid can order/compare it in SQL.</summary>
    public double CraftingCost { get; set; }

    public DateTimeOffset ComputedAtUtc { get; set; }
}
