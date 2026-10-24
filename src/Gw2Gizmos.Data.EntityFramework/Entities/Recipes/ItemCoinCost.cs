using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

/// <summary>
/// What one of an item the trading post doesn't buy costs to get for coin alone: the cheapest of buying it from a
/// vendor and making it, with account currencies (karma, Fractal Relics, …) and parts coin can't buy counted as
/// nothing. Holdings with no buy
/// order are valued at it (see <see cref="AccountHoldingsReconstructor.ItemValues"/>), as gw2efficiency values
/// untradeable items at their crafting cost. The worker rebuilds it on each commerce refresh for the items accounts
/// hold, replacing it wholesale; absence means nothing of it costs coin.
/// </summary>
[Table("ItemCoinCosts")]
public class ItemCoinCost
{
    /// <summary>The GW2 item id; the natural key.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }

    /// <summary>The cost of one, in copper.</summary>
    public double Cost { get; set; }

    public DateTimeOffset ComputedAtUtc { get; set; }
}
