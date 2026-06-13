using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

/// <summary>
/// A precomputed snapshot row backing the desktop Market grid: one row per tradeable item (anything with
/// a trading-post listing), carrying the current best buy/sell prices and — for craftable items — the
/// cheapest craft cost, profit, and the serialized craft tree. The worker rebuilds the whole table on each
/// commerce refresh and replaces it wholesale, so the grid is a cheap read with no per-row computation.
/// <para>
/// Money figures are stored as <c>double</c> (SQLite REAL) so the grid can order/compare them in SQL.
/// Craft fields are null when the item has no recipe, or has one whose cost can't be fully priced. The
/// craft tree itself is not stored — it's rebuilt on demand for the single selected row, which is cheap
/// (the builder memoizes), and avoids persisting megabytes of deep trees that change every hour.
/// </para>
/// </summary>
[Table("MarketItems")]
public class MarketItem
{
    /// <summary>The GW2 item id; the natural key (one row per tradeable item, not database-generated).</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }

    [MaxLength(256)]
    public string Name { get; set; } = "";

    /// <summary>Highest standing buy order (copper); null when nobody is buying.</summary>
    public int? Buy { get; set; }

    /// <summary>Total units wanted across all buy orders (the in-game "Demand").</summary>
    public int Demand { get; set; }

    /// <summary>Lowest standing sell listing (copper); null when nothing is listed.</summary>
    public int? Sell { get; set; }

    /// <summary>Total units listed for sale across all sell orders (the in-game "Supply").</summary>
    public int Supply { get; set; }

    /// <summary>True when a recipe outputs this item (regardless of whether its cost is fully known).</summary>
    public bool IsCraftable { get; set; }

    /// <summary>Cheapest cost to craft one, walking the ingredient tree (copper); null when not fully priced.</summary>
    public double? CraftingCost { get; set; }

    /// <summary>Profit from crafting then selling into buy orders, after the 15% fee; null when not craftable.</summary>
    public double? Profit { get; set; }

    /// <summary>Profit as a percentage of crafting cost; null when not craftable.</summary>
    public double? MarginPercent { get; set; }

    public DateTimeOffset ComputedAtUtc { get; set; }
}
