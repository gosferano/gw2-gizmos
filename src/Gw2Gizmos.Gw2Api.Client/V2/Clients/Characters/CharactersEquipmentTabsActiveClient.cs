using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public sealed class CharactersEquipmentTabsActiveClient
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
