using Gw2Gizmos.Gw2Api.Contract.V2.Currencies;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Currencies;

public sealed class CurrenciesClient : BaseBulkAllClient<Currency, int>, ICurrenciesClient
{
    internal CurrenciesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/currencies";
}
