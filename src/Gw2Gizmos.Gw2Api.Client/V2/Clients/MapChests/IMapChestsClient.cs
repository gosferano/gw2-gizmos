using Gw2Gizmos.Gw2Api.Contract.MapChests;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.MapChests;

public interface IMapChestsClient
    : IAllExpandableClient<MapChest>,
        IBulkExpandableClient<MapChest, string>,
        IPaginatedClient<MapChest>;
