using Gw2Gizmos.Gw2Api.Contract.Races;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Races;

public class RacesClient : BaseBulkAllClient<Race, string>, IRacesClient
{
    internal RacesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/races";
}
