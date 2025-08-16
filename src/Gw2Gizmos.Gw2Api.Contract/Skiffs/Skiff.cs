namespace Gw2Gizmos.Gw2Api.Contract.Skiffs;

public class Skiff
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public SkinDyeSlot[] DyeSlots { get; set; } = Array.Empty<SkinDyeSlot>();
}
