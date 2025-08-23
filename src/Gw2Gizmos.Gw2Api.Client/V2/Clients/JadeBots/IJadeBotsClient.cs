using Gw2Gizmos.Gw2Api.Contract.V2.JadeBots;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.JadeBots;

public interface IJadeBotsClient
    : IAllExpandableClient<JadeBot>,
        IBulkExpandableClient<JadeBot, int>,
        IPaginatedClient<JadeBot>;
