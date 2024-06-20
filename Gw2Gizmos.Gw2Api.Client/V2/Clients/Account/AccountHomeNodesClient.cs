namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountHomeNodesClient : BaseBlobClient<string[]>, IAccountHomeNodesClient
{
    internal AccountHomeNodesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/home/nodes";
}
