using Gw2Gizmos.Gw2Api.Contract.Legends;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Legends;

public interface ILegendsClient
    : IAllExpandableClient<Legend>,
        IBulkExpandableClient<Legend, string>,
        IPaginatedClient<Legend>;
