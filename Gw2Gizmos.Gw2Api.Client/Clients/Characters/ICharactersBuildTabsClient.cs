using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersBuildTabsClient
    : IAllExpandableClient<CharacterBuildTab>,
        IBulkExpandableClient<CharacterBuildTab, int>,
        IPaginatedClient<CharacterBuildTab>
{
    public ICharactersBuildTabsActiveClient Active { get; }
}
