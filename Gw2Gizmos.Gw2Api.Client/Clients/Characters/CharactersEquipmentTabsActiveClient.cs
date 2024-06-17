using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersEquipmentTabsActiveClient
    : BaseBlobClient<CharacterEquipmentTab>,
        ICharactersEquipmentTabsActiveClient
{
    private readonly string _characterId;

    internal CharactersEquipmentTabsActiveClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/equipmenttabs/active";
}
