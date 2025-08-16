using Gw2Gizmos.Gw2Api.Contract.Dungeons;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Dungeons;

public class DungeonsClient : BaseBulkAllClient<Dungeon, string>, IDungeonsClient
{
    internal DungeonsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/dungeons";
}
