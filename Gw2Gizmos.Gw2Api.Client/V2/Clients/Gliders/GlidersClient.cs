using Gw2Gizmos.Gw2Api.Contract.Gliders;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Gliders;

public class GlidersClient : BaseBulkAllClient<Glider, int>, IGlidersClient
{
    internal GlidersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/gliders";
}
