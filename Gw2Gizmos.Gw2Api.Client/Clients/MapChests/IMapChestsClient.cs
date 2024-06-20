using Gw2Gizmos.Gw2Api.Contract.MapChests;

namespace Gw2Gizmos.Gw2Api.Client.Clients.MapChests;

public interface IMapChestsClient
    : IAllExpandableClient<MapChest>,
        IBulkExpandableClient<MapChest, string>,
        IPaginatedClient<MapChest>;
