using Gw2Gizmos.Gw2Api.Contract.BuildStorage;

namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class CharacterBuildTab
{
    public int Tab { get; set; }
    public bool IsActive { get; set; }
    public BuildStorageBuild Build { get; set; }
}
