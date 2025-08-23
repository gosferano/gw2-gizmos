using Gw2Gizmos.Gw2Api.Contract.V2.Guild;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public class GuildUpgradesClient : BaseBulkAllClient<GuildUpgrade, int>, IGuildUpgradesClient
{
    internal GuildUpgradesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/guild/upgrades";
}
