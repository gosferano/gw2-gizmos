using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwRanksClient
    : IAllExpandableClient<WvwRank>,
        IBulkExpandableClient<WvwRank, int>,
        IPaginatedClient<WvwRank>;
