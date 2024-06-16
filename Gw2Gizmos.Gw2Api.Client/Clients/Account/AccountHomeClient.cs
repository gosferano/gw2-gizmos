namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public class AccountHomeClient : BaseBlobClient<string[]>, IAccountHomeClient
{
    internal AccountHomeClient(HttpClient httpClient)
        : base(httpClient)
    {
        Cats = new AccountHomeCatsClient(httpClient);
        Nodes = new AccountHomeNodesClient(httpClient);
    }

    protected override string UriPath => "/v2/account/home";
    public IAccountHomeCatsClient Cats { get; }
    public IAccountHomeNodesClient Nodes { get; }
}
