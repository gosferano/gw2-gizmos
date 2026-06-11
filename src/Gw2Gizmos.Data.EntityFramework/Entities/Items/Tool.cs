using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Tools")]
public class Tool : Item
{
    // Navigation
    public ToolDetails Details { get; set; } = null!;
}

[Table("ToolDetails")]
public class ToolDetails
{
    [Key, ForeignKey(nameof(Tool))]
    public int ItemId { get; set; }
    public string Type { get; set; } = null!;
    public int Charges { get; set; }

    // Navigation
    public Tool Tool { get; set; } = null!;
}
