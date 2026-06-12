using Gw2Gizmos.Gw2Api.Contract.V2.Skiffs;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skiffs;

public sealed class SkiffsClient : BaseBulkAllClient<Skiff, int>, ISkiffsClient
{
    internal SkiffsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/skiffs";
}
