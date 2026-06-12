namespace Gw2Gizmos.Gw2Api.Contract.V2.Mounts;

public sealed class MountType
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int DefaultSkin { get; set; }
    public int[] Skins { get; set; } = Array.Empty<int>();
    public MountTypeSkill[] Skills { get; set; } = Array.Empty<MountTypeSkill>();
}

public sealed class MountTypeSkill
{
    public int Id { get; set; }
    public string Slot { get; set; } = null!;
}
