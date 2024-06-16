using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdEquipmentClient : BaseBlobClient<CharacterEquipment>, ICharactersIdEquipmentClient
{
    private readonly string _characterId;

    internal CharactersIdEquipmentClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/equipment";
}
