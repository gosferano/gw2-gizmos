using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public class ContinentsClient : BaseBulkAllClient<Continent, int>, IContinentsClient
{
    internal ContinentsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/continents";

    public IContinentsIdClient this[int continentId] => new ContinentsIdClient(HttpClient, continentId);
}
