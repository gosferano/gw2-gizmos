using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("InfixUpgradeAttributes")]
public class InfixUpgradeAttribute
{
    public int Id { get; set; }

    public string Attribute { get; set; } = string.Empty;
    public decimal Modifier { get; set; }

    public int InfixUpgradeId { get; set; }
    public InfixUpgrade InfixUpgrade { get; set; } = null!;
}
