namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterBuildTabBuild
{
    public string Name { get; set; }
    public string Profession { get; set; }
    public CharacterBuildTabSpecialization[] Specializations { get; set; } =
        Array.Empty<CharacterBuildTabSpecialization>();
    public CharacterBuildTabSkills Skills { get; set; }
    public CharacterBuildTabSkills AquaticSkills { get; set; }
    public string[]? Legends { get; set; }
    public string[]? AquaticLegends { get; set; }
    public CharacterBuildTabPets? Pets { get; set; }
}
