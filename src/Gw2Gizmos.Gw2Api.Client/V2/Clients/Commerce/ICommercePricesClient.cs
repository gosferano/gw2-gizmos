using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommercePricesClient
    : IBulkExpandableClient<CommercePrices, int>,
        IPaginatedClient<CommercePrices> { }
