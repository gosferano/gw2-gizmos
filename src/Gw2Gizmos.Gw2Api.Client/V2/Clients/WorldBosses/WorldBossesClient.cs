using Gw2Gizmos.Gw2Api.Contract.V2.WorldBosses;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.WorldBosses;

public class WorldBossesClient : BaseBulkAllClient<WorldBoss, string>, IWorldBossesClient
{
    internal WorldBossesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/worldbosses";
}
