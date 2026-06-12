namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountSkiffsClient : BaseBlobClient<int[]>, IAccountSkiffsClient
{
    internal AccountSkiffsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/skiffs";
}
