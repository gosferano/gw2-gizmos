namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountClient : BaseBlobClient<Contract.Account.Account>, IAccountClient
{
    internal AccountClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Achievements = new AccountAchievementsClient(apiClient);
        Bank = new AccountBankClient(apiClient);
        DailyCrafting = new AccountDailyCraftingClient(apiClient);
        Dyes = new AccountDyesClient(apiClient);
        Dungeons = new AccountDungeonsClient(apiClient);
        Materials = new AccountMaterialsClient(apiClient);
    }

    protected override string UriPath => "/v2/account";

    public IAccountAchievementsClient Achievements { get; }
    public IAccountBankClient Bank { get; }
    public IAccountDailyCraftingClient DailyCrafting { get; }
    public IAccountDyesClient Dyes { get; }
    public IAccountDungeonsClient Dungeons { get; }
    public IAccountMaterialsClient Materials { get; }
}
