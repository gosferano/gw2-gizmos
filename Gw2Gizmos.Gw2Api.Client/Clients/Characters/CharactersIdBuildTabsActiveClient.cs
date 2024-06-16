using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdBuildTabsActiveClient : BaseBlobClient<CharacterBuildTab>, ICharactersIdBuildTabsActiveClient
{
    private readonly string _characterId;

    internal CharactersIdBuildTabsActiveClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/buildtabs/active";
}
