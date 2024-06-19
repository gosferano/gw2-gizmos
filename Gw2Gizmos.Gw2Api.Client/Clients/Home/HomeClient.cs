namespace Gw2Gizmos.Gw2Api.Client.Clients.Home;

public class HomeClient : BaseBlobClient<string[]>, IHomeClient
{
    internal HomeClient(HttpClient httpClient)
        : base(httpClient)
    {
        Cats = new HomeCatsClient(httpClient);
        Nodes = new HomeNodesClient(httpClient);
    }

    protected override string UriPath => "/v2/home";

    public IHomeCatsClient Cats { get; }
    public IHomeNodesClient Nodes { get; }
}
