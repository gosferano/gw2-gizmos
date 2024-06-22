using Gw2Gizmos.Gw2Api.Contract.Traits;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Traits;

public class TraitsClient : BaseBulkAllClient<Trait, int>, ITraitsClient
{
    internal TraitsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/traits";
}
