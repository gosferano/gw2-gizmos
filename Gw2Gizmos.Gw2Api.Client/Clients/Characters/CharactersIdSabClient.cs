using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdSabClient : BaseBlobClient<CharacterSab>, ICharactersIdSabClient
{
    private readonly string _characterId;

    internal CharactersIdSabClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/sab";
}
