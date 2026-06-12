using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public sealed class CharactersEquipmentTabsClient
    : BaseBulkAllClient<CharacterEquipmentTab, int>,
        ICharactersEquipmentTabsClient
{
    private readonly string _characterId;

    internal CharactersEquipmentTabsClient(HttpClient httpClient, string characterId)
        : base(httpClient, "tabs")
    {
        _characterId = characterId;
        Active = new CharactersEquipmentTabsActiveClient(httpClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/equipmenttabs";

    public ICharactersEquipmentTabsActiveClient Active { get; }
}
