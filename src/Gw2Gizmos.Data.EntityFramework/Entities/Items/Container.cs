using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Containers")]
public class Container : Item
{
    // Navigation
    public ContainerDetails Details { get; set; } = null!;
}

[Table("ContainerDetails")]
public class ContainerDetails
{
    [Key, ForeignKey(nameof(Container))]
    public int ItemId { get; set; }
    public string Type { get; set; } = null!;

    // Navigation
    public Container Container { get; set; } = null!;
}
