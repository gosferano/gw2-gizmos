using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

public abstract class ItemInfusionSlot
{
    [Key]
    public int Id { get; set; }

    public int? ItemId { get; set; }

    public List<ItemInfusionSlotFlag> Flags { get; set; } = [];
}

[Table("ArmorInfusionSlots")]
public class ArmorInfusionSlot : ItemInfusionSlot
{
    public int ArmorDetailsId { get; set; }
    public ArmorDetails ArmorDetails { get; set; } = null!;
}

[Table("BackItemInfusionSlots")]
public class BackItemInfusionSlot : ItemInfusionSlot
{
    public int BackItemDetailsId { get; set; }
    public BackItemDetails BackItemDetails { get; set; } = null!;
}

[Table("TrinketInfusionSlots")]
public class TrinketInfusionSlot : ItemInfusionSlot
{
    public int TrinketDetailsId { get; set; }
    public TrinketDetails TrinketDetails { get; set; } = null!;
}

[Table("WeaponInfusionSlots")]
public class WeaponInfusionSlot : ItemInfusionSlot
{
    public int WeaponDetailsId { get; set; }
    public WeaponDetails WeaponDetails { get; set; } = null!;
}
