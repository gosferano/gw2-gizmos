using Gw2Gizmos.Gw2Api.Contract.Stories;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Stories;

public interface IStoriesSeasonsClient
    : IAllExpandableClient<StorySeason>,
        IBulkExpandableClient<StorySeason, string>,
        IPaginatedClient<StorySeason>;
