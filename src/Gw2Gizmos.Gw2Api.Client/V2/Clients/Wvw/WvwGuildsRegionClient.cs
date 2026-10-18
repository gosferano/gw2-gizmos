using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwGuildsRegionClient : BaseBlobClient<WvwGuildTeams>, IWvwGuildsRegionClient
{
    private readonly WvwRegion _region;

    internal WvwGuildsRegionClient(HttpClient httpClient, WvwRegion region)
        : base(httpClient)
    {
        _region = region;
    }

    protected override string UriPath => $"/v2/wvw/guilds/{_region}";
}
