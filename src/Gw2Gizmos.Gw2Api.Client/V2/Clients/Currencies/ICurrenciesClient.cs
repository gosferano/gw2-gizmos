using Gw2Gizmos.Gw2Api.Contract.V2.Currencies;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Currencies;

public interface ICurrenciesClient
    : IAllExpandableClient<Currency>,
        IBulkExpandableClient<Currency, int>,
        IPaginatedClient<Currency>;
