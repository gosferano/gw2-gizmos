using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Items")]
public class Item
{
    public int Id { get; set; }
    public string ChatLink { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public string Rarity { get; set; } = null!;
    public int Level { get; set; }
    public int VendorValue { get; set; }
    public int? DefaultSkin { get; set; }
    public List<ItemFlag> Flags { get; set; } = [];
    public List<ItemGameType> GameTypes { get; set; } = [];
    public List<ItemRestriction> Restrictions { get; set; } = [];
}
