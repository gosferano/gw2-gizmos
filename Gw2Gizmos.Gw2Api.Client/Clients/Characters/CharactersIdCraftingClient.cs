using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdCraftingClient : BaseBlobClient<CharacterCrafting>, ICharactersIdCraftingClient
{
    private readonly string _characterId;

    internal CharactersIdCraftingClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient)
    {
        _characterId = characterId;
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/crafting";
}
