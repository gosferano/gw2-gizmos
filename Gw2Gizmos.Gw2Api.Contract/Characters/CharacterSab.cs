namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterSab
{
    public CharacterSabZone[] Zones { get; set; } = Array.Empty<CharacterSabZone>();
    public CharacterSabSong[] Songs { get; set; } = Array.Empty<CharacterSabSong>();
    public CharacterSabUnlock[] Unlocks { get; set; } = Array.Empty<CharacterSabUnlock>();
}
