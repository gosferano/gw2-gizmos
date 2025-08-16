using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public class PvpStandingsClient : BaseBlobClient<PvpStanding[]>, IPvpStandingsClient
{
    internal PvpStandingsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/pvp/standings";
}
