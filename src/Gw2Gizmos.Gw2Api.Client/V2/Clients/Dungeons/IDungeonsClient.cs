using Gw2Gizmos.Gw2Api.Contract.V2.Dungeons;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Dungeons;

public interface IDungeonsClient
    : IAllExpandableClient<Dungeon>,
        IBulkExpandableClient<Dungeon, string>,
        IPaginatedClient<Dungeon>;
