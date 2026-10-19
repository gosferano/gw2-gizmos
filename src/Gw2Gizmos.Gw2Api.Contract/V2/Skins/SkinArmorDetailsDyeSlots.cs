namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

public sealed class SkinArmorDetailsDyeSlots
{
    /// <summary>The skin's dye slots, a slot null where it can't be dyed.</summary>
    public SkinDyeSlot?[] Default { get; set; } = Array.Empty<SkinDyeSlot?>();
    public SkinArmorDetailsDyeSlotsOverrides Overrides { get; set; } = null!;
}
