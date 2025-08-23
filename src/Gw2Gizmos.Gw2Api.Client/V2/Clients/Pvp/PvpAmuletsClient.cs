using Gw2Gizmos.Gw2Api.Contract.V2.Pvp;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pvp;

public class PvpAmuletsClient : BaseBulkAllClient<PvpAmulet, int>, IPvpAmuletsClient
{
    internal PvpAmuletsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/pvp/amulets";
}
