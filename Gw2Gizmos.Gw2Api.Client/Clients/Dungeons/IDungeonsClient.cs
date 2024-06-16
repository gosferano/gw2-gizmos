using Gw2Gizmos.Gw2Api.Contract.Dungeons;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Dungeons;

public interface IDungeonsClient
    : IAllExpandableClient<Dungeon>,
        IBulkExpandableClient<Dungeon, string>,
        IPaginatedClient<Dungeon>;
