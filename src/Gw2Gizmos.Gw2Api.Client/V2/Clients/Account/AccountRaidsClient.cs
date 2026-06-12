namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountRaidsClient : BaseBlobClient<string[]>, IAccountRaidsClient
{
    internal AccountRaidsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/raids";
}
