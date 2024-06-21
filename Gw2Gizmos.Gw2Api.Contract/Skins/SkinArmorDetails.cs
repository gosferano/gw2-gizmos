using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Contract.Skins;

public class SkinArmorDetails
{
    public ArmorSlotType Type { get; set; }
    public ArmorWeightClass WeightClass { get; set; }
    public SkinArmorDetailsDyeSlots DyeSlots { get; set; }
}
