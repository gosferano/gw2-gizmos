using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Trinkets")]
public class Trinket : Item
{
    // Navigation
    public TrinketDetails Details { get; set; }
}

[Table("TrinketDetails")]
public class TrinketDetails
{
    [Key, ForeignKey(nameof(Trinket))]
    public int ItemId { get; set; }
    public string Type { get; set; }
    public decimal AttributeAdjustment { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }

    // Navigation
    public Trinket Trinket { get; set; }

    public List<TrinketInfusionSlot> InfusionSlots { get; set; } = new();
    public List<TrinketStatChoice> StatChoices { get; set; } = new();

    // Navigation: principal side
    [InverseProperty(nameof(InfixUpgrade.TrinketDetails))]
    public TrinketInfixUpgrade? InfixUpgrade { get; set; }
}
