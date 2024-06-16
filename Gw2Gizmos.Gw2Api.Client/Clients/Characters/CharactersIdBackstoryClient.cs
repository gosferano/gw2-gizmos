using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdBackstoryClient : BaseBlobClient<CharacterBackstory>, ICharactersIdBackstoryClient
{
    private readonly string _characterId;

    internal CharactersIdBackstoryClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/backstory";
}
