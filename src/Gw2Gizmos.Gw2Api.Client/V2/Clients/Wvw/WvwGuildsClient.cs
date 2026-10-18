using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwGuildsClient : BaseBlobClient<string[]>, IWvwGuildsClient
{
    internal WvwGuildsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/guilds";

    public IWvwGuildsRegionClient this[WvwRegion region] => new WvwGuildsRegionClient(HttpClient, region);
}
