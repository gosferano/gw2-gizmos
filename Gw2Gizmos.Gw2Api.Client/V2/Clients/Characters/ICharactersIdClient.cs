namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public interface ICharactersIdClient
{
    public ICharactersBackstoryClient Backstory { get; }
    public ICharactersBuildTabsClient BuildTabs { get; }
    public ICharactersCoreClient Core { get; }
    public ICharactersCraftingClient Crafting { get; }
    public ICharactersEquipmentClient Equipment { get; }
    public ICharactersEquipmentTabsClient EquipmentTabs { get; }
    public ICharactersHeroPointsClient HeroPoints { get; }
    public ICharactersInventoryClient Inventory { get; set; }
    public ICharactersQuestsClient Quests { get; }
    public ICharactersRecipesClient Recipes { get; }
    public ICharactersSabClient Sab { get; }
    public ICharactersTrainingClient Training { get; }
}
