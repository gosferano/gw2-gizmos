using Gw2Gizmos.Gw2Api.Contract.Skiffs;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skiffs;

public interface ISkiffsClient
    : IAllExpandableClient<Skiff>,
        IBulkExpandableClient<Skiff, int>,
        IPaginatedClient<Skiff>;
