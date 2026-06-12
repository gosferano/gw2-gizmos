namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountHomeCatsClient : BaseBlobClient<int[]>, IAccountHomeCatsClient
{
    internal AccountHomeCatsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/home/cats";
}
