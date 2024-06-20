using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public class CharactersCraftingClient : BaseBlobClient<CharacterCrafting>, ICharactersCraftingClient
{
    private readonly string _characterId;

    internal CharactersCraftingClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/crafting";
}
