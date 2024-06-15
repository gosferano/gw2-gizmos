using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdBackstoryClient : BaseBlobClient<CharacterBackstory>, ICharactersIdBackstoryClient
{
    private readonly string _characterId;

    internal CharactersIdBackstoryClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/backstory";
}
