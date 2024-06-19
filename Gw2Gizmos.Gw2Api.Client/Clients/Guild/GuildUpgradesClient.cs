using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildUpgradesClient : BaseBulkAllClient<GuildUpgrade, int>, IGuildUpgradesClient
{
    internal GuildUpgradesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guild/upgrades";
}
