namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public class CharacterSabZone
{
    public int Id { get; set; }
    public string Mode { get; set; } = null!;
    public int World { get; set; }
    public int Zone { get; set; }
}
