using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwMatchesScoresClient
    : IAllExpandableClient<WvwMatchScores>,
        IBulkExpandableClient<WvwMatchScores, string>,
        IPaginatedClient<WvwMatchScores>
{
    public IWvwMatchesScoresWorldClient WithWorldId(int worldId);
}
