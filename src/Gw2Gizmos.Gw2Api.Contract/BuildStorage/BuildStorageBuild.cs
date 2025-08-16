namespace Gw2Gizmos.Gw2Api.Contract.BuildStorage;

public class BuildStorageBuild
{
    public string Name { get; set; }
    public string Profession { get; set; }
    public BuildStorageSpecialization[] Specializations { get; set; } = Array.Empty<BuildStorageSpecialization>();
    public BuildStorageSkills Skills { get; set; }
    public BuildStorageSkills AquaticSkills { get; set; }
    public string[]? Legends { get; set; }
    public string[]? AquaticLegends { get; set; }
    public BuildStoragePets? Pets { get; set; }
}
