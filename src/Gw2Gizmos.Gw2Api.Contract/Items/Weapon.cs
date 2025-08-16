namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Weapon : Item
{
    public WeaponDetails Details { get; set; }
}

public class WeaponDetails
{
    public WeaponType Type { get; set; }
    public WeaponDamageType DamageType { get; set; }
    public int MinPower { get; set; }
    public int MaxPower { get; set; }
    public int Defense { get; set; }
    public InfusionSlot[] InfusionSlots { get; set; } = Array.Empty<InfusionSlot>();
    public decimal AttributeAdjustment { get; set; }
    public InfixUpgrade? InfixUpgrade { get; set; }
    public int? SuffixItemId { get; set; }
    public int? SecondarySuffixItemId { get; set; }
    public int[] StatChoices { get; set; } = Array.Empty<int>();
}
