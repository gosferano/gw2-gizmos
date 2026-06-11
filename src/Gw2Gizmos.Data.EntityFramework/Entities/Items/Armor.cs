using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Armors")]
public class Armor : Item
{
    public ArmorDetails Details { get; set; } = null!;
}

[Table("ArmorDetails")]
public class ArmorDetails
{
    [Key, ForeignKey(nameof(Armor))] // <-- tells EF this is also the FK
    public int ItemId { get; set; }
    public string Type { get; set; } = null!;
    public string WeightClass { get; set; } = null!;
    public int Defense { get; set; }
    public decimal AttributeAdjustment { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }

    // Navigation
    public Armor Armor { get; set; } = null!;

    public List<ArmorInfusionSlot> InfusionSlots { get; set; } = new();
    public List<ArmorStatChoice> StatChoices { get; set; } = new();

    // Navigation: principal side
    [InverseProperty(nameof(InfixUpgrade.ArmorDetails))]
    public ArmorInfixUpgrade? InfixUpgrade { get; set; }
}
