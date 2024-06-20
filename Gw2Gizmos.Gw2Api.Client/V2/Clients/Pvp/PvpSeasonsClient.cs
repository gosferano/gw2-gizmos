using Gw2Gizmos.Gw2Api.Contract.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public class PvpSeasonsClient : BaseBulkAllClient<PvpSeason, string>, IPvpSeasonsClient
{
    internal PvpSeasonsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/pvp/seasons";
}
