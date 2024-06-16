namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountDungeonsClient : BaseBlobClient<string[]>, IAccountDungeonsClient
{
    internal AccountDungeonsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/account/dungeons";
}
