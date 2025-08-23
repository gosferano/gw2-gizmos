using Gw2Gizmos.Gw2Api.Contract.V2.ItemStats;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.ItemStats;

public class ItemStatsClient : BaseBulkAllClient<ItemStat, int>, IItemStatsClient
{
    internal ItemStatsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/itemstats";
}
