using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdEquipmentTabsClient
    : BaseBulkAllClient<CharacterEquipmentTab, int>,
        ICharactersIdEquipmentTabsClient
{
    private readonly string _characterId;

    internal CharactersIdEquipmentTabsClient(HttpClient httpClient, string characterId)
        : base(httpClient, "tabs")
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/equipmenttabs";
}
