using Gw2Gizmos.Gw2Api.Contract.JadeBots;

namespace Gw2Gizmos.Gw2Api.Client.Clients.JadeBots;

public interface IJadeBotsClient
    : IAllExpandableClient<JadeBot>,
        IBulkExpandableClient<JadeBot, int>,
        IPaginatedClient<JadeBot>;
