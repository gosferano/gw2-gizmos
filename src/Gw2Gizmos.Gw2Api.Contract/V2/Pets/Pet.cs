namespace Gw2Gizmos.Gw2Api.Contract.V2.Pets;

public class Pet
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public PetSkill[] Skills { get; set; } = Array.Empty<PetSkill>();
}
