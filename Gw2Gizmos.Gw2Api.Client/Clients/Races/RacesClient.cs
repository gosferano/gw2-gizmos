using Gw2Gizmos.Gw2Api.Contract.Races;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Races;

public class RacesClient : BaseBulkAllClient<Race, string>, IRacesClient
{
    internal RacesClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/races";
}
