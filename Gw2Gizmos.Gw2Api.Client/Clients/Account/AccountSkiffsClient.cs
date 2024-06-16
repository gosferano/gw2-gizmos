namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountSkiffsClient : BaseBlobClient<int[]>, IAccountSkiffsClient
{
    internal AccountSkiffsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/skiffs";
}
