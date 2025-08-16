using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

public abstract class ItemStatChoiceBase
{
    [Key]
    public int Id { get; set; }
    public int StatId { get; set; }
}

[Table("ArmorStatChoices")]
public class ArmorStatChoice : ItemStatChoiceBase
{
    public int ArmorDetailsId { get; set; }
    public ArmorDetails ArmorDetails { get; set; }
}

[Table("BackItemStatChoices")]
public class BackItemStatChoice : ItemStatChoiceBase
{
    public int BackItemDetailsId { get; set; }
    public BackItemDetails BackItemDetails { get; set; }
}

[Table("TrinketStatChoices")]
public class TrinketStatChoice : ItemStatChoiceBase
{
    public int TrinketDetailsId { get; set; }
    public TrinketDetails TrinketDetails { get; set; }
}

[Table("WeaponStatChoices")]
public class WeaponStatChoice : ItemStatChoiceBase
{
    public int WeaponDetailsId { get; set; }
    public WeaponDetails WeaponDetails { get; set; }
}
