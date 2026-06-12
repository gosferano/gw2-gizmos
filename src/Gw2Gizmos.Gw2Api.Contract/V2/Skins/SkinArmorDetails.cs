using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

public sealed class SkinArmorDetails
{
    public ArmorSlotType Type { get; set; }
    public ArmorWeightClass WeightClass { get; set; }
    public SkinArmorDetailsDyeSlots? DyeSlots { get; set; }
}
