namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountClient : BaseBlobClient<Contract.Account.Account>, IAccountClient
{
    internal AccountClient(HttpClient httpClient)
        : base(httpClient)
    {
        Achievements = new AccountAchievementsClient(httpClient);
        Bank = new AccountBankClient(httpClient);
        DailyCrafting = new AccountDailyCraftingClient(httpClient);
        Dungeons = new AccountDungeonsClient(httpClient);
        Dyes = new AccountDyesClient(httpClient);
        Emotes = new AccountEmotesClient(httpClient);
        Finishers = new AccountFinishersClient(httpClient);
        Gliders = new AccountGlidersClient(httpClient);
        Materials = new AccountMaterialsClient(httpClient);
    }

    protected override string UriPath => "/v2/account";

    public IAccountAchievementsClient Achievements { get; }
    public IAccountBankClient Bank { get; }
    public IAccountDailyCraftingClient DailyCrafting { get; }
    public IAccountDungeonsClient Dungeons { get; }
    public IAccountDyesClient Dyes { get; }
    public IAccountEmotesClient Emotes { get; }
    public IAccountFinishersClient Finishers { get; }
    public IAccountGlidersClient Gliders { get; }
    public IAccountMaterialsClient Materials { get; }
}
