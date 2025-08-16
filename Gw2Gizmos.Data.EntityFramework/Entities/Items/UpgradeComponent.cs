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
    public InfixUpgrade InfixUpgrade { get; set; }
}
