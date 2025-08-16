using Gw2Gizmos.Gw2Api.Contract.ItemStats;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.ItemStats;

public interface IItemStatsClient
    : IAllExpandableClient<ItemStat>,
        IBulkExpandableClient<ItemStat, int>,
        IPaginatedClient<ItemStat>;
