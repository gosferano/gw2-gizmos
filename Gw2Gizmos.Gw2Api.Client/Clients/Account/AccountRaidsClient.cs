namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountRaidsClient : BaseBlobClient<string[]>, IAccountRaidsClient
{
    internal AccountRaidsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/raids";
}
