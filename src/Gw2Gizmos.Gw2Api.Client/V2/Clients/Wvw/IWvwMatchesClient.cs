using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwMatchesClient
    : IAllExpandableClient<WvwMatch>,
        IBulkExpandableClient<WvwMatch, string>,
        IPaginatedClient<WvwMatch>
{
    public IWvwMatchesScoresClient Scores { get; }
    public IWvwMatchesStatsClient Stats { get; }
    public IWvwMatchesOverviewClient Overview { get; }
}
