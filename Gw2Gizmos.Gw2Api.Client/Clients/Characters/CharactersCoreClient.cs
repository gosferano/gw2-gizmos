using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersCoreClient : BaseBlobClient<CharacterCore>, ICharactersCoreClient
{
    private readonly string _characterId;

    internal CharactersCoreClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/core";
}
