namespace Gw2Gizmos.Gw2Api.Contract.V2.Skiffs;

public class Skiff
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public SkinDyeSlot[] DyeSlots { get; set; } = Array.Empty<SkinDyeSlot>();
}
