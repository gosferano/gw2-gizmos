using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("UpgradeComponents")]
public class UpgradeComponent : Item
{
    // Navigation
    public UpgradeComponentDetails Details { get; set; } = null!;
}

[Table("UpgradeComponentDetails")]
public class UpgradeComponentDetails
{
    [Key, ForeignKey(nameof(UpgradeComponent))]
    public int ItemId { get; set; }
    public string Type { get; set; } = null!;
    public string Suffix { get; set; } = null!;

    // Navigation
    public UpgradeComponent UpgradeComponent { get; set; } = null!;

    public List<string> Flags { get; set; } = [];
    public List<string> InfusionUpgradeFlags { get; set; } = [];
    public List<string> Bonuses { get; set; } = [];

    // FK lives on the InfixUpgrade side (UpgradeComponentInfixUpgrade.UpgradeComponentDetailsId),
    // consistent with the gear types — so removing the Details cascades to the InfixUpgrade.
    [InverseProperty(nameof(InfixUpgrade.UpgradeComponentDetails))]
    public UpgradeComponentInfixUpgrade? InfixUpgrade { get; set; }
}
