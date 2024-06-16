using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdCoreClient : BaseBlobClient<CharacterCore>, ICharactersIdCoreClient
{
    private readonly string _characterId;

    internal CharactersIdCoreClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/core";
}
