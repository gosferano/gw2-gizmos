using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwRanksClient : BaseBulkAllClient<WvwRank, int>, IWvwRanksClient
{
    internal WvwRanksClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/ranks";
}
