using Gw2Gizmos.Gw2Api.Contract.Home;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Home;

public class HomeCatsClient : BaseBulkAllClient<HomeCat, int>, IHomeCatsClient
{
    internal HomeCatsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/home/cats";
}
