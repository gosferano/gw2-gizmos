using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommercePricesClient
    : IBulkExpandableClient<CommercePrices, int>,
        IPaginatedClient<CommercePrices> { }
