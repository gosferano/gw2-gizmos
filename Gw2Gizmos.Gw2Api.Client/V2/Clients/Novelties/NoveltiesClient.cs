using Gw2Gizmos.Gw2Api.Contract.Novelties;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Novelties;

public class NoveltiesClient : BaseBulkAllClient<Novelty, int>, INoveltiesClient
{
    internal NoveltiesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/novelties";
}
