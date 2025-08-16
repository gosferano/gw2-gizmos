using Gw2Gizmos.Gw2Api.Contract.Home;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Home;

public interface IHomeNodesClient
    : IAllExpandableClient<HomeNode>,
        IBulkExpandableClient<HomeNode, string>,
        IPaginatedClient<HomeNode>;
