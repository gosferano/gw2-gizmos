using Gw2Gizmos.Gw2Api.Contract.V2.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public sealed class ContinentsClient : BaseBulkAllClient<Continent, int>, IContinentsClient
{
    internal ContinentsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/continents";

    public IContinentsIdClient this[int continentId] => new ContinentsIdClient(HttpClient, continentId);
}
