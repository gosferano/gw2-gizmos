using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

/// <summary>
/// One of an account's open trading-post orders (<c>/v2/commerce/transactions/current/{buys,sells}</c>), with the
/// item's best competing prices as of the sync that stored it — enough to tell whether the order has been undercut
/// (a cheaper sell listing) or outbid (a higher buy order). An account's rows are replaced on every sync.
/// </summary>
[Table("TradingPostOrders")]
[Index(nameof(AccountId))]
public class TradingPostOrder
{
    /// <summary>The API's transaction id.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary><see cref="TradingPostSide.Buy"/> or <see cref="TradingPostSide.Sell"/>.</summary>
    [MaxLength(8)]
    public string Side { get; set; } = "";

    public int ItemId { get; set; }

    /// <summary>The order's unit price, in copper.</summary>
    public int Price { get; set; }

    /// <summary>Units still open on the order.</summary>
    public int Quantity { get; set; }

    /// <summary>When the order was placed.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>The item's highest buy order at sync time (copper); null when nobody is buying.</summary>
    public int? BestBuyPrice { get; set; }

    /// <summary>The item's lowest sell listing at sync time (copper); null when nothing is listed.</summary>
    public int? BestSellPrice { get; set; }

    /// <summary>When this snapshot of the order (and its competing prices) was taken.</summary>
    public DateTimeOffset SyncedAtUtc { get; set; }
}
