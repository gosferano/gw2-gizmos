namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountDailyCraftingClient : BaseBlobClient<string[]>, IAccountDailyCraftingClient
{
    internal AccountDailyCraftingClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/account/dailycrafting";
}
