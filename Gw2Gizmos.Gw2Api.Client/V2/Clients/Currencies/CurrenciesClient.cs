using Gw2Gizmos.Gw2Api.Contract.Currencies;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Currencies;

public class CurrenciesClient : BaseBulkAllClient<Currency, int>, ICurrenciesClient
{
    internal CurrenciesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/currencies";
}
