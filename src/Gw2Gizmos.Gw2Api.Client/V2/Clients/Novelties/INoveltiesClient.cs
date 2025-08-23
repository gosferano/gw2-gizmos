using Gw2Gizmos.Gw2Api.Contract.V2.Novelties;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Novelties;

public interface INoveltiesClient
    : IAllExpandableClient<Novelty>,
        IBulkExpandableClient<Novelty, int>,
        IPaginatedClient<Novelty>;
