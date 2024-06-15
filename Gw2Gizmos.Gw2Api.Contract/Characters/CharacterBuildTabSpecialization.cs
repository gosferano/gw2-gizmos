namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterBuildTabSpecialization
{
    public int Id { get; set; }
    public int[] Traits { get; set; } = Array.Empty<int>();
}
