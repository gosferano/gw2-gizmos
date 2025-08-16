using Gw2Gizmos.Gw2Api.Contract.MapChests;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.MapChests;

public class MapChestsClient : BaseBulkAllClient<MapChest, string>, IMapChestsClient
{
    internal MapChestsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/mapchests";
}
