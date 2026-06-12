using Gw2Gizmos.Gw2Api.Contract.V2.Home;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Home;

public sealed class HomeNodesClient : BaseBulkAllClient<HomeNode, string>, IHomeNodesClient
{
    public HomeNodesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/home/nodes";
}
