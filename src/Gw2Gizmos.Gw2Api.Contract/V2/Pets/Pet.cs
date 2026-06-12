namespace Gw2Gizmos.Gw2Api.Contract.V2.Pets;

public sealed class Pet
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public PetSkill[] Skills { get; set; } = Array.Empty<PetSkill>();
}
