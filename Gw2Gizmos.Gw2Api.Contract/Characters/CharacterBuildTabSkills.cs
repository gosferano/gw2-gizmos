namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterBuildTabSkills
{
    public int? Heal { get; set; }
    public int?[] Utilities { get; set; } = Array.Empty<int?>();
    public int? Elite { get; set; }
}
