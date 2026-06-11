using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Commerce;

[Table("CommerceItemListings")]
public class CommerceItemListing
{
    [Key, ForeignKey("Item")]
    public int ItemId { get; set; }

    public ICollection<BuyListing> Buys { get; set; } = new List<BuyListing>();

    public ICollection<SellListing> Sells { get; set; } = new List<SellListing>();

    // Navigation
    public Item Item { get; set; } = null!;
}
