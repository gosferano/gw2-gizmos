using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Bags")]
public class Bag : Item
{
    public BagDetails Details { get; set; }
}

[Table("BagDetails")]
public class BagDetails
{
    [Key, ForeignKey(nameof(Bag))] // <-- tells EF this is also the FK
    public int ItemId { get; set; }
    public int Size { get; set; }
    public bool NoSellOrSort { get; set; }

    // Navigation
    public Bag Bag { get; set; }
}
