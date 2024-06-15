namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdClient : BaseClient, ICharactersIdClient
{
    private readonly string _characterId;

    internal CharactersIdClient(IGw2ApiClient apiClient, string characterId)
        : base(apiClient)
    {
        _characterId = characterId;
        Backstory = new CharactersIdBackstoryClient(apiClient, characterId);
        BuildTabs = new CharactersIdBuildTabsClient(apiClient, characterId);
        Core = new CharactersIdCoreClient(apiClient, characterId);
        Crafting = new CharactersIdCraftingClient(apiClient, characterId);
        Equipment = new CharactersIdEquipmentClient(apiClient, characterId);
        EquipmentTabs = new CharactersIdEquipmentTabsClient(apiClient, characterId);
        Inventory = new CharactersIdInventoryClient(apiClient, characterId);
        Recipes = new CharactersIdRecipesClient(apiClient, characterId);
        Training = new CharactersIdTrainingClient(apiClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}";

    public ICharactersIdBackstoryClient Backstory { get; }
    public ICharactersIdBuildTabsClient BuildTabs { get; set; }
    public ICharactersIdCoreClient Core { get; set; }
    public ICharactersIdCraftingClient Crafting { get; set; }
    public ICharactersIdEquipmentClient Equipment { get; }
    public ICharactersIdEquipmentTabsClient EquipmentTabs { get; set; }
    public ICharactersIdInventoryClient Inventory { get; set; }
    public ICharactersIdRecipesClient Recipes { get; }
    public ICharactersIdTrainingClient Training { get; }
}
