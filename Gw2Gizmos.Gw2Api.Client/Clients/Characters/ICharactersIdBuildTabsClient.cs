using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersIdBuildTabsClient
    : IAllExpandableClient<CharacterBuildTab>,
        IBulkExpandableClient<CharacterBuildTab, int>,
        IPaginatedClient<CharacterBuildTab>
{
    public ICharactersIdBuildTabsActiveClient Active { get; }
}
