using Gw2Gizmos.Gw2Api.Contract.Traits;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Traits;

public interface ITraitsClient
    : IAllExpandableClient<Trait>,
        IBulkExpandableClient<Trait, int>,
        IPaginatedClient<Trait>;
