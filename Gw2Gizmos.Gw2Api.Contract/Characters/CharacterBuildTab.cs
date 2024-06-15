namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterBuildTab
{
    public int Tab { get; set; }
    public bool IsActive { get; set; }
    public CharacterBuildTabBuild Build { get; set; }
}
