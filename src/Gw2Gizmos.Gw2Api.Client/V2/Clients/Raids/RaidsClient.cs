using Gw2Gizmos.Gw2Api.Contract.V2.Raids;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Raids;

public class RaidsClient : BaseBulkAllClient<Raid, string>, IRaidsClient
{
    internal RaidsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/raids";
}
