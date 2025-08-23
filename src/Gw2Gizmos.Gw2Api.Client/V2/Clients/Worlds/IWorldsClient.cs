using Gw2Gizmos.Gw2Api.Contract.V2.Worlds;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Worlds;

public interface IWorldsClient
    : IAllExpandableClient<World>,
        IBulkExpandableClient<World, int>,
        IPaginatedClient<World>;
