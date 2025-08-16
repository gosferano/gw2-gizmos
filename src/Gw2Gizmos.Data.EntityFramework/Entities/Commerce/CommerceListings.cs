using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

public abstract class CommerceListing
{
    [Key]
    public int Id { get; set; }

    public int Listings { get; set; }

    public int UnitPrice { get; set; }

    public int Quantity { get; set; }

    public int CommerceItemListingId { get; set; }

    [ForeignKey("CommerceItemListingId")]
    public CommerceItemListing CommerceItemListing { get; set; } = null!;
}

[Table("BuyListings")]
public class BuyListing : CommerceListing { }

[Table("SellListings")]
public class SellListing : CommerceListing { }
