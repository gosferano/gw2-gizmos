using Gw2Gizmos.Gw2Api.Contract.Legends;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Legends;

public interface ILegendsClient
    : IAllExpandableClient<Legend>,
        IBulkExpandableClient<Legend, string>,
        IPaginatedClient<Legend>;
