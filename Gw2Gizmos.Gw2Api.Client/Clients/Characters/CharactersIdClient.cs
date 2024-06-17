namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdClient : BaseClient, ICharactersIdClient
{
    private readonly string _characterId;

    internal CharactersIdClient(HttpClient httpClient, string characterId)
        : base(httpClient)
    {
        _characterId = characterId;
        Backstory = new CharactersBackstoryClient(httpClient, characterId);
        BuildTabs = new CharactersBuildTabsClient(httpClient, characterId);
        Core = new CharactersCoreClient(httpClient, characterId);
        Crafting = new CharactersCraftingClient(httpClient, characterId);
        Equipment = new CharactersEquipmentClient(httpClient, characterId);
        EquipmentTabs = new CharactersEquipmentTabsClient(httpClient, characterId);
        HeroPoints = new CharactersHeroPointsClient(httpClient, characterId);
        Inventory = new CharactersInventoryClient(httpClient, characterId);
        Quests = new CharactersQuestsClient(httpClient, characterId);
        Recipes = new CharactersRecipesClient(httpClient, characterId);
        Sab = new CharactersSabClient(httpClient, characterId);
        Training = new CharactersTrainingClient(httpClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}";

    public ICharactersBackstoryClient Backstory { get; }
    public ICharactersBuildTabsClient BuildTabs { get; set; }
    public ICharactersCoreClient Core { get; set; }
    public ICharactersCraftingClient Crafting { get; set; }
    public ICharactersEquipmentClient Equipment { get; }
    public ICharactersEquipmentTabsClient EquipmentTabs { get; set; }
    public ICharactersHeroPointsClient HeroPoints { get; }
    public ICharactersInventoryClient Inventory { get; set; }
    public ICharactersQuestsClient Quests { get; }
    public ICharactersRecipesClient Recipes { get; }
    public ICharactersSabClient Sab { get; }
    public ICharactersTrainingClient Training { get; }
}
