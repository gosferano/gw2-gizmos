namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountClient : BaseBlobClient<Contract.Account.Account>, IAccountClient
{
    internal AccountClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Achievements = new AccountAchievementsClient(apiClient);
        Bank = new AccountBankClient(apiClient);
    }

    protected override string UriPath => "/v2/account";

    public AccountAchievementsClient Achievements { get; }
    public AccountBankClient Bank { get; }
}
