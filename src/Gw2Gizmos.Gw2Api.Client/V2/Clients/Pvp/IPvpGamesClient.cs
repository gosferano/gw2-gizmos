using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpGamesClient
    : IAllExpandableClient<PvpGame>,
        IBulkExpandableClient<PvpGame, string>,
        IPaginatedClient<PvpGame>;
