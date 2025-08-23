namespace Gw2Gizmos.Gw2Api.Contract.V2.Mounts;

public class MountType
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int DefaultSkin { get; set; }
    public int[] Skins { get; set; } = Array.Empty<int>();
    public MountTypeSkill[] Skills { get; set; } = Array.Empty<MountTypeSkill>();
}

public class MountTypeSkill
{
    public int Id { get; set; }
    public string Slot { get; set; }
}
