namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

public class SkinArmorDetailsDyeSlots
{
    public SkinDyeSlot[] Default { get; set; } = Array.Empty<SkinDyeSlot>();
    public SkinArmorDetailsDyeSlotsOverrides Overrides { get; set; } = null!;
}
