using Gw2Gizmos.Gw2Api.Contract.ItemStats;

namespace Gw2Gizmos.Gw2Api.Client.Clients.ItemStats;

public interface IItemStatsClient
    : IAllExpandableClient<ItemStat>,
        IBulkExpandableClient<ItemStat, int>,
        IPaginatedClient<ItemStat>;
