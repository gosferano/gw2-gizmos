using Gw2Gizmos.Gw2Api.Contract.Gliders;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Gliders;

public interface IGlidersClient
    : IAllExpandableClient<Glider>,
        IBulkExpandableClient<Glider, int>,
        IPaginatedClient<Glider>;
