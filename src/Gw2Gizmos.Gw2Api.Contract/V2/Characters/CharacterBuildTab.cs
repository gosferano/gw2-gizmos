using Gw2Gizmos.Gw2Api.Contract.V2.BuildStorage;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public class CharacterBuildTab
{
    public int Tab { get; set; }
    public bool IsActive { get; set; }
    public BuildStorageBuild Build { get; set; }
}
