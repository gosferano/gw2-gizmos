using Gw2Gizmos.Gw2Api.Contract.Account;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountAchievementsClient : BaseBlobClient<AccountAchievement[]>, IAccountAchievementsClient
{
    internal AccountAchievementsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/account/achievements";
}
