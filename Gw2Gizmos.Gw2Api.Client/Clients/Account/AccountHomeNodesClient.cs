namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountHomeNodesClient : BaseBlobClient<string[]>, IAccountHomeNodesClient
{
    internal AccountHomeNodesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/home/nodes";
}
