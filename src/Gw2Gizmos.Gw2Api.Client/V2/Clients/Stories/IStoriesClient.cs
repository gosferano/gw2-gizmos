using Gw2Gizmos.Gw2Api.Contract.V2.Stories;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Stories;

public interface IStoriesClient
    : IAllExpandableClient<Story>,
        IBulkExpandableClient<Story, int>,
        IPaginatedClient<Story>
{
    public IStoriesSeasonsClient Seasons { get; }
}
