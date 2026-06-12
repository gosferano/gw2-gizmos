using Gw2Gizmos.Gw2Api.Contract.V2.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public sealed class CharactersInventoryClient : BaseBlobClient<CharacterInventory>, ICharactersInventoryClient
{
    private readonly string _characterId;

    internal CharactersInventoryClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/inventory";
}
