using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceTransactionsCurrentBuysClient
    : IBlobClient<CommerceTransaction[]>,
        IPaginatedClient<CommerceTransaction> { }
