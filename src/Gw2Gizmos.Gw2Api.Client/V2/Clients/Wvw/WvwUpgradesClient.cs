using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwUpgradesClient : BaseBulkAllClient<WvwUpgrade, int>, IWvwUpgradesClient
{
    internal WvwUpgradesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/upgrades";
}
