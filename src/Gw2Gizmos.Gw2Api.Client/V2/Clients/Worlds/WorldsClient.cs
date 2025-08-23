using Gw2Gizmos.Gw2Api.Contract.V2.Worlds;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Worlds;

public class WorldsClient : BaseBulkAllClient<World, int>, IWorldsClient
{
    internal WorldsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/worlds";
}
