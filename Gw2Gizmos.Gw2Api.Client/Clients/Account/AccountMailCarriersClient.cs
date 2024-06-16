namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountMailCarriersClient : BaseBlobClient<int[]>, IAccountMailCarriersClient
{
    internal AccountMailCarriersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/mailcarriers";
}
