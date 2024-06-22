using Gw2Gizmos.Gw2Api.Contract.WorldBosses;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.WorldBosses;

public interface IWorldBossesClient
    : IAllExpandableClient<WorldBoss>,
        IBulkExpandableClient<WorldBoss, string>,
        IPaginatedClient<WorldBoss>;
