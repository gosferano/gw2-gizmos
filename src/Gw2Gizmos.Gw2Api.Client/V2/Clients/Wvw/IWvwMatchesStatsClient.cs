using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwMatchesStatsClient
    : IAllExpandableClient<WvwMatchStats>,
        IBulkExpandableClient<WvwMatchStats, string>,
        IPaginatedClient<WvwMatchStats>
{
    public IWvwMatchesStatsWorldClient WithWorldId(int worldId);
}
