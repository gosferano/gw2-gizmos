namespace Gw2Gizmos.Gw2Api.Contract.Mounts;

public class MountSkin
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Mount { get; set; }
    public SkinDyeSlot[] DyeSlots { get; set; } = Array.Empty<SkinDyeSlot>();
}
