namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersIdClient
{
    public ICharactersIdBackstoryClient Backstory { get; }
    public ICharactersIdBuildTabsClient BuildTabs { get; }
    public ICharactersIdCoreClient Core { get; }
    public ICharactersIdCraftingClient Crafting { get; }
    public ICharactersIdEquipmentClient Equipment { get; }
    public ICharactersIdEquipmentTabsClient EquipmentTabs { get; }
}
