using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Weapons")]
public class Weapon : Item
{
    public WeaponDetails Details { get; set; } = null!;
}

[Table("WeaponDetails")]
public class WeaponDetails
{
    [Key, ForeignKey(nameof(Weapon))] // <-- tells EF this is also the FK
    public int ItemId { get; set; }

    public string Type { get; set; } = string.Empty; // map from WeaponType enum
    public string DamageType { get; set; } = string.Empty; // map from WeaponDamageType enum

    public int MinPower { get; set; }
    public int MaxPower { get; set; }
    public int Defense { get; set; }
    public decimal AttributeAdjustment { get; set; }

    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }

    // Navigation
    public Weapon Weapon { get; set; } = null!;

    // Navigation properties
    public List<WeaponInfusionSlot> InfusionSlots { get; set; } = [];
    public List<WeaponStatChoice> StatChoices { get; set; } = [];

    // Navigation: principal side
    [InverseProperty(nameof(InfixUpgrade.WeaponDetails))]
    public WeaponInfixUpgrade? InfixUpgrade { get; set; }
}
