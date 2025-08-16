using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public interface IPvpSeasonsClient
    : IAllExpandableClient<PvpSeason>,
        IBulkExpandableClient<PvpSeason, string>,
        IPaginatedClient<PvpSeason>
{
    public IPvpSeasonsIdClient this[string seasonId] { get; }
}
