using Gw2Gizmos.Gw2Api.Contract.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwMatchesOverviewClient
    : IAllExpandableClient<WvwMatchOverview>,
        IBulkExpandableClient<WvwMatchOverview, string>,
        IPaginatedClient<WvwMatchOverview>
{
    public IWvwMatchesOverviewWorldClient WithWorldId(int worldId);
}
