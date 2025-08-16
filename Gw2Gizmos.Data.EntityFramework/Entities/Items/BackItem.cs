using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("BackItems")]
public class BackItem : Item
{
    public BackItemDetails Details { get; set; }
}

[Table("BackItemDetails")]
public class BackItemDetails
{
    [Key, ForeignKey(nameof(BackItem))]
    public int ItemId { get; set; }

    // Navigation
    public BackItem BackItem { get; set; }

    // Properties from contract
    public decimal AttributeAdjustment { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }

    // Navigation properties
    public List<BackItemInfusionSlot> InfusionSlots { get; set; } = new();
    public List<BackItemStatChoice> StatChoices { get; set; } = new();

    // Navigation: principal side
    [InverseProperty(nameof(InfixUpgrade.BackItemDetails))]
    public BackItemInfixUpgrade? InfixUpgrade { get; set; }
}
