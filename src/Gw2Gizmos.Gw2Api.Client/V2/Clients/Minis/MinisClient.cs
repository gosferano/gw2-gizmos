using Gw2Gizmos.Gw2Api.Contract.V2.Minis;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Minis;

public sealed class MinisClient : BaseBulkAllClient<Mini, int>, IMinisClient
{
    internal MinisClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/minis";
}
