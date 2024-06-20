namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountDailyCraftingClient : BaseBlobClient<string[]>, IAccountDailyCraftingClient
{
    internal AccountDailyCraftingClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/dailycrafting";
}
