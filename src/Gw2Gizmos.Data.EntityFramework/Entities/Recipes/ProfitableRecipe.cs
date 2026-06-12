using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

/// <summary>
/// A precomputed snapshot row for the desktop "Profitable" feed: one craftable item whose craft cost
/// (after walking its full ingredient tree) sits below what it sells for on the trading post. The worker
/// recomputes the whole snapshot on each commerce cadence and replaces this table wholesale.
/// <para>
/// The summary columns exist so the grid can sort and filter without deserializing anything. The full
/// craft tree is persisted as serialized JSON in <see cref="TreeJson"/> (a <c>RecipeNode</c> from the
/// RecipeFinder engine) and is only parsed when a row is selected — keeping the domain model out of the
/// persistence layer while still letting the UI render the tree however it likes.
/// </para>
/// </summary>
[Table("ProfitableRecipes")]
public class ProfitableRecipe
{
    public int Id { get; set; }

    public int OutputItemId { get; set; }

    [MaxLength(256)]
    public string OutputItemName { get; set; } = "";

    /// <summary>
    /// Cheapest cost to craft one batch, walking the ingredient tree (copper). Stored as a double (SQLite
    /// REAL) rather than decimal so the snapshot can be ordered/compared in SQL — exactness doesn't matter
    /// for a derived figure the worker recomputes every hour.
    /// </summary>
    public double CraftingCost { get; set; }

    /// <summary>Trading-post sell price for the configured side of the book (copper).</summary>
    public int SellPrice { get; set; }

    /// <summary>Trading-post buy price for the configured side of the book (copper).</summary>
    public int BuyPrice { get; set; }

    /// <summary>Profit after the 15% trading-post fee: <c>SellPrice * 0.85 - CraftingCost</c> (copper).</summary>
    public double Profit { get; set; }

    /// <summary>Profit as a percentage of crafting cost; 0 when the cost is unknown.</summary>
    public double MarginPercent { get; set; }

    public DateTimeOffset ComputedAtUtc { get; set; }

    /// <summary>The full crafting tree (serialized <c>RecipeNode</c>), parsed lazily by the UI on selection.</summary>
    public string TreeJson { get; set; } = "";
}
