using Gw2Gizmos.Gw2Api.Contract.Worlds;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Worlds;

public interface IWorldsClient
    : IAllExpandableClient<World>,
        IBulkExpandableClient<World, int>,
        IPaginatedClient<World>;
