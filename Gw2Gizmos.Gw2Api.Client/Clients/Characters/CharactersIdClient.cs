namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdClient : BaseClient, ICharactersIdClient
{
    private readonly string _characterId;

    internal CharactersIdClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
        Backstory = new CharactersIdBackstoryClient(httpClient, characterId);
        BuildTabs = new CharactersIdBuildTabsClient(httpClient, characterId);
        Core = new CharactersIdCoreClient(httpClient, characterId);
        Crafting = new CharactersIdCraftingClient(httpClient, characterId);
        Equipment = new CharactersIdEquipmentClient(httpClient, characterId);
        EquipmentTabs = new CharactersIdEquipmentTabsClient(httpClient, characterId);
        HeroPoints = new CharactersIdHeroPointsClient(httpClient, characterId);
        Inventory = new CharactersIdInventoryClient(httpClient, characterId);
        Quests = new CharactersIdQuestsClient(httpClient, characterId);
        Recipes = new CharactersIdRecipesClient(httpClient, characterId);
        Sab = new CharactersIdSabClient(httpClient, characterId);
        Training = new CharactersIdTrainingClient(httpClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}";

    public ICharactersIdBackstoryClient Backstory { get; }
    public ICharactersIdBuildTabsClient BuildTabs { get; set; }
    public ICharactersIdCoreClient Core { get; set; }
    public ICharactersIdCraftingClient Crafting { get; set; }
    public ICharactersIdEquipmentClient Equipment { get; }
    public ICharactersIdEquipmentTabsClient EquipmentTabs { get; set; }
    public ICharactersIdHeroPointsClient HeroPoints { get; }
    public ICharactersIdInventoryClient Inventory { get; set; }
    public ICharactersIdQuestsClient Quests { get; }
    public ICharactersIdRecipesClient Recipes { get; }
    public ICharactersIdSabClient Sab { get; }
    public ICharactersIdTrainingClient Training { get; }
}
