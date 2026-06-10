using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("UpgradeComponents")]
public class UpgradeComponent : Item
{
    // Navigation
    public UpgradeComponentDetails Details { get; set; }
}

[Table("UpgradeComponentDetails")]
public class UpgradeComponentDetails
{
    [Key, ForeignKey(nameof(UpgradeComponent))]
    public int ItemId { get; set; }
    public string Type { get; set; }
    public string Suffix { get; set; }

    // Navigation
    public UpgradeComponent UpgradeComponent { get; set; }

    public List<string> Flags { get; set; } = [];
    public List<string> InfusionUpgradeFlags { get; set; } = [];
    public List<string> Bonuses { get; set; } = [];

    // FK lives on the InfixUpgrade side (UpgradeComponentInfixUpgrade.UpgradeComponentDetailsId),
    // consistent with the gear types — so removing the Details cascades to the InfixUpgrade.
    [InverseProperty(nameof(InfixUpgrade.UpgradeComponentDetails))]
    public UpgradeComponentInfixUpgrade? InfixUpgrade { get; set; }
}
