using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("ItemInfusionSlotFlags")]
public class ItemInfusionSlotFlag
{
    public int Id { get; set; }

    public string Flag { get; set; } = string.Empty;

    public int InfusionSlotId { get; set; }
    public ItemInfusionSlot InfusionSlot { get; set; } = null!;
}
