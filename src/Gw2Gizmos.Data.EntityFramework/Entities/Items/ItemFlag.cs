using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("ItemFlags")]
public class ItemFlag
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Value { get; set; } = "";

    public Item Item { get; set; } = null!;
}
