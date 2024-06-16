using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdBuildTabsClient : BaseBulkAllClient<CharacterBuildTab, int>, ICharactersIdBuildTabsClient
{
    private readonly string _characterId;

    internal CharactersIdBuildTabsClient(HttpClient httpClient, string characterId)
        : base(httpClient, "tabs")
    {
        _characterId = characterId;
        Active = new CharactersIdBuildTabsActiveClient(httpClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/buildtabs";

    public ICharactersIdBuildTabsActiveClient Active { get; }
}
