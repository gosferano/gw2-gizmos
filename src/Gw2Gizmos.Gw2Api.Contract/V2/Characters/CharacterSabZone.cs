namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public sealed class CharacterSabZone
{
    public int Id { get; set; }
    public string Mode { get; set; } = null!;
    public int World { get; set; }
    public int Zone { get; set; }
}
