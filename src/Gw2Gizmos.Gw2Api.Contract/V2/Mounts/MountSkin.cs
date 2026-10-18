namespace Gw2Gizmos.Gw2Api.Contract.V2.Mounts;

public sealed class MountSkin
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    /// <summary>
    /// The same for every skin of a mount, but not exposed by the mount types. To find a skin's mount, look it up in
    /// the mount types' skins instead.
    /// </summary>
    public string MountGuid { get; set; } = null!;
    public SkinDyeSlot[] DyeSlots { get; set; } = Array.Empty<SkinDyeSlot>();
}
