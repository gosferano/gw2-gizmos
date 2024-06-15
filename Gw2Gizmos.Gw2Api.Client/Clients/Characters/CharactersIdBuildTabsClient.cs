using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdBuildTabsClient : BaseBulkAllClient<CharacterBuildTab, int>, ICharactersIdBuildTabsClient
{
    private readonly string _characterId;

    internal CharactersIdBuildTabsClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient, "tabs")
    {
        _characterId = characterId;
        Active = new CharactersIdBuildTabsActiveClient(apiClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/buildtabs";

    public ICharactersIdBuildTabsActiveClient Active { get; }
}
