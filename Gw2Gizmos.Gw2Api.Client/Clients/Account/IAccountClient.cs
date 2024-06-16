namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountClient : IBlobClient<Contract.Account.Account>
{
    public IAccountAchievementsClient Achievements { get; }
    public IAccountBankClient Bank { get; }
    public IAccountBuildStorageClient BuildStorage { get; }
    public IAccountDailyCraftingClient DailyCrafting { get; }
    public IAccountDungeonsClient Dungeons { get; }
    public IAccountDyesClient Dyes { get; }
    public IAccountEmotesClient Emotes { get; }
    public IAccountFinishersClient Finishers { get; }
    public IAccountGlidersClient Gliders { get; }
    public IAccountHomeClient Home { get; }
    public IAccountInventoryClient Inventory { get; }
    public IAccountJadeBotsClient JadeBots { get; }
    public IAccountLegendaryArmoryClient LegendaryArmory { get; }
    public IAccountLuckClient Luck { get; }
    public IAccountMailCarriersClient MailCarriers { get; }
    public IAccountMapChestsClient MapChests { get; }
    public IAccountMasteriesClient Masteries { get; }
    public IAccountMinisClient Minis { get; }
    public IAccountMountsClient Mounts { get; }
    public IAccountMaterialsClient Materials { get; }
    public IAccountNoveltiesClient Novelties { get; }
    public IAccountOutfitsClient Outfits { get; }
    public IAccountProgressionClient Progression { get; }
    public IAccountPvpClient Pvp { get; }
    public IAccountRaidsClient Raids { get; }
    public IAccountRecipesClient Recipes { get; }
    public IAccountSkiffsClient Skiffs { get; }
    public IAccountSkinsClient Skins { get; }
    public IAccountTitlesClient Titles { get; }
    public IAccountWalletClient Wallet { get; }
    public IAccountWorldBossesClient WorldBosses { get; }
}
