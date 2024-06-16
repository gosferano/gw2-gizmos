namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountClient : BaseBlobClient<Contract.Account.Account>, IAccountClient
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
        Inventory = new AccountInventoryClient(httpClient);
        JadeBots = new AccountJadeBotsClient(httpClient);
        LegendaryArmory = new AccountLegendaryArmoryClient(httpClient);
        Luck = new AccountLuckClient(httpClient);
        MailCarriers = new AccountMailCarriersClient(httpClient);
        MapChests = new AccountMapChestsClient(httpClient);
        Masteries = new AccountMasteriesClient(httpClient);
        Materials = new AccountMaterialsClient(httpClient);
        Minis = new AccountMinisClient(httpClient);
        Mounts = new AccountMountsClient(httpClient);
        Novelties = new AccountNoveltiesClient(httpClient);
        Outfits = new AccountOutfitsClient(httpClient);
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
}
