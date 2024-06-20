using Gw2Gizmos.Gw2Api.Contract.Maps;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Maps;

public class MapsClient : BaseBulkAllClient<Map, int>, IMapsClient
{
    internal MapsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/maps";
}
