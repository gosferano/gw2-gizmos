using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpRanksClient
    : IAllExpandableClient<PvpRank>,
        IBulkExpandableClient<PvpRank, int>,
        IPaginatedClient<PvpRank>;
