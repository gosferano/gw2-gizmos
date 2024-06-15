using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceListingsClient
    : IBulkExpandableClient<CommerceListings, int>,
        IPaginatedClient<CommerceListings> { }
