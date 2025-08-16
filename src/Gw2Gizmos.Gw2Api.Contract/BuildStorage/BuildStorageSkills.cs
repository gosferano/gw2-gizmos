namespace Gw2Gizmos.Gw2Api.Contract.BuildStorage;

public class BuildStorageSkills
{
    public int? Heal { get; set; }
    public int?[] Utilities { get; set; } = Array.Empty<int?>();
    public int? Elite { get; set; }
}
