namespace Gw2Gizmos.Gw2Api.Contract.V2.BuildStorage;

public sealed class BuildStorageSkills
{
    public int? Heal { get; set; }
    public int?[] Utilities { get; set; } = Array.Empty<int?>();
    public int? Elite { get; set; }
}
