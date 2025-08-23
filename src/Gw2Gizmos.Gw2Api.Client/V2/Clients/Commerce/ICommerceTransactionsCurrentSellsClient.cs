using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceTransactionsCurrentSellsClient
    : IBlobClient<CommerceTransaction[]>,
        IPaginatedClient<CommerceTransaction> { }
