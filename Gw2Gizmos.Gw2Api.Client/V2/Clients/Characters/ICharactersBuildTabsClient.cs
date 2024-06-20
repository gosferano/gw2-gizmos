using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public interface ICharactersBuildTabsClient
    : IAllExpandableClient<CharacterBuildTab>,
        IBulkExpandableClient<CharacterBuildTab, int>,
        IPaginatedClient<CharacterBuildTab>
{
    public ICharactersBuildTabsActiveClient Active { get; }
}
