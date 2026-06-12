namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountMailCarriersClient : BaseBlobClient<int[]>, IAccountMailCarriersClient
{
    internal AccountMailCarriersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mailcarriers";
}
