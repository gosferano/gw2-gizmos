namespace Gw2Gizmos.Gw2Api.Contract.V2.BuildStorage;

public sealed class BuildStorageBuild
{
    public string Name { get; set; } = null!;
    public string Profession { get; set; } = null!;
    public BuildStorageSpecialization[] Specializations { get; set; } = Array.Empty<BuildStorageSpecialization>();
    public BuildStorageSkills Skills { get; set; } = null!;
    public BuildStorageSkills AquaticSkills { get; set; } = null!;
    public string[]? Legends { get; set; }
    public string[]? AquaticLegends { get; set; }
    public BuildStoragePets? Pets { get; set; }
}
