using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

public abstract class InfixUpgrade
{
    public int Id { get; set; }

    // One-to-many attributes
    public List<InfixUpgradeAttribute> Attributes { get; set; } = [];

    // One-to-one Buff
    public InfixUpgradeBuff? Buff { get; set; }
}

[Table("ArmorInfixUpgrades")]
public class ArmorInfixUpgrade : InfixUpgrade
{
    public int ArmorDetailsId { get; set; }

    [ForeignKey(nameof(ArmorDetailsId))]
    public ArmorDetails ArmorDetails { get; set; }
}

[Table("BackItemInfixUpgrades")]
public class BackItemInfixUpgrade : InfixUpgrade
{
    public int BackItemDetailsId { get; set; }

    [ForeignKey(nameof(BackItemDetailsId))]
    public BackItemDetails BackItemDetails { get; set; }
}

[Table("TrinketInfixUpgrades")]
public class TrinketInfixUpgrade : InfixUpgrade
{
    public int TrinketDetailsId { get; set; }

    [ForeignKey(nameof(TrinketDetailsId))]
    public TrinketDetails TrinketDetails { get; set; }
}

[Table("UpgradeComponentInfixUpgrades")]
public class UpgradeComponentInfixUpgrade : InfixUpgrade
{
    public int UpgradeComponentDetailsId { get; set; }

    [ForeignKey(nameof(UpgradeComponentDetailsId))]
    public UpgradeComponentDetails UpgradeComponentDetails { get; set; }
}

[Table("WeaponInfixUpgrades")]
public class WeaponInfixUpgrade : InfixUpgrade
{
    public int WeaponDetailsId { get; set; }

    [ForeignKey(nameof(WeaponDetailsId))]
    public WeaponDetails WeaponDetails { get; set; }
}
