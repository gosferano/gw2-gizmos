using Gw2Gizmos.Gw2Api.Contract.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwAbilitiesClient : BaseBulkAllClient<WvwAbility, int>, IWvwAbilitiesClient
{
    internal WvwAbilitiesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/abilities";
}
