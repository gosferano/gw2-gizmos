using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

/// <summary>
/// A completed trade — something the account bought or sold on the trading post
/// (<c>/v2/commerce/transactions/history/{buys,sells}</c>). The API only keeps 90 days, so rows are kept here for
/// good: each sync adds the trades not seen before, and the in-app log reads from this table.
/// </summary>
[Table("TradingPostTransactions")]
[Index(nameof(AccountId), nameof(PurchasedUtc))]
public class TradingPostTransaction
{
    /// <summary>The API's transaction id.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary><see cref="TradingPostSide.Buy"/> (the account bought) or <see cref="TradingPostSide.Sell"/>.</summary>
    [MaxLength(8)]
    public string Side { get; set; } = "";

    public int ItemId { get; set; }

    /// <summary>The unit price, in copper.</summary>
    public int Price { get; set; }

    public int Quantity { get; set; }

    /// <summary>When the order was placed.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>When the trade completed.</summary>
    public DateTimeOffset PurchasedUtc { get; set; }
}
