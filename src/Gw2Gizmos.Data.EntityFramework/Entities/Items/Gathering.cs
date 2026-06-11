using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Gatherings")]
public class Gathering : Item
{
    // Navigation
    public GatheringDetails Details { get; set; } = null!;
}

[Table("GatheringDetails")]
public class GatheringDetails
{
    [Key, ForeignKey(nameof(Gathering))]
    public int ItemId { get; set; }
    public string Type { get; set; } = null!;

    // Navigation
    public Gathering Gathering { get; set; } = null!;
}
