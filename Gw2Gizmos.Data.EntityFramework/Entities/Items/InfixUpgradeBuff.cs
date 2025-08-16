using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("InfixUpgradeBuffs")]
public class InfixUpgradeBuff
{
    public int Id { get; set; }

    public int SkillId { get; set; }
    public string? Description { get; set; }

    public int InfixUpgradeId { get; set; }
    public InfixUpgrade InfixUpgrade { get; set; } = null!;
}
