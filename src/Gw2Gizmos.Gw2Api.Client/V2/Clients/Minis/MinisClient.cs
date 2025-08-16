using Gw2Gizmos.Gw2Api.Contract.Minis;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Minis;

public class MinisClient : BaseBulkAllClient<Mini, int>, IMinisClient
{
    internal MinisClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/minis";
}
