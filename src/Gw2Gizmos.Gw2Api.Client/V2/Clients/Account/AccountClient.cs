namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountClient : BaseBlobClient<Contract.V2.Account.Account>, IAccountClient
{
    internal AccountClient(HttpClient httpClient)
        : base(httpClient)
    {
        Achievements = new AccountAchievementsClient(httpClient);
        Bank = new AccountBankClient(httpClient);
        BuildStorage = new AccountBuildStorageClient(httpClient);
        DailyCrafting = new AccountDailyCraftingClient(httpClient);
        Dungeons = new AccountDungeonsClient(httpClient);
        Dyes = new AccountDyesClient(httpClient);
        Emotes = new AccountEmotesClient(httpClient);
        Finishers = new AccountFinishersClient(httpClient);
        Gliders = new AccountGlidersClient(httpClient);
        Home = new AccountHomeClient(httpClient);
        Homestead = new AccountHomesteadClient(httpClient);
        Inventory = new AccountInventoryClient(httpClient);
        JadeBots = new AccountJadeBotsClient(httpClient);
        LegendaryArmory = new AccountLegendaryArmoryClient(httpClient);
        Luck = new AccountLuckClient(httpClient);
        MailCarriers = new AccountMailCarriersClient(httpClient);
        MapChests = new AccountMapChestsClient(httpClient);
        Masteries = new AccountMasteriesClient(httpClient);
        Mastery = new AccountMasteryClient(httpClient);
        Materials = new AccountMaterialsClient(httpClient);
        Minis = new AccountMinisClient(httpClient);
        Mounts = new AccountMountsClient(httpClient);
        Novelties = new AccountNoveltiesClient(httpClient);
        Outfits = new AccountOutfitsClient(httpClient);
        Progression = new AccountProgressionClient(httpClient);
        Pvp = new AccountPvpClient(httpClient);
        Raids = new AccountRaidsClient(httpClient);
        Recipes = new AccountRecipesClient(httpClient);
        Skiffs = new AccountSkiffsClient(httpClient);
        Skins = new AccountSkinsClient(httpClient);
        Titles = new AccountTitlesClient(httpClient);
        Wallet = new AccountWalletClient(httpClient);
        WizardsVault = new AccountWizardsVaultClient(httpClient);
        WorldBosses = new AccountWorldBossesClient(httpClient);
        Wvw = new AccountWvwClient(httpClient);
    }

    protected override string UriPath => "/v2/account";

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
    public IAccountHomesteadClient Homestead { get; }
    public IAccountInventoryClient Inventory { get; }
    public IAccountJadeBotsClient JadeBots { get; }
    public IAccountLegendaryArmoryClient LegendaryArmory { get; }
    public IAccountLuckClient Luck { get; }
    public IAccountMailCarriersClient MailCarriers { get; }
    public IAccountMapChestsClient MapChests { get; }
    public IAccountMasteriesClient Masteries { get; }
    public IAccountMasteryClient Mastery { get; }
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
    public IAccountWizardsVaultClient WizardsVault { get; }
    public IAccountWorldBossesClient WorldBosses { get; }
    public IAccountWvwClient Wvw { get; }
}
