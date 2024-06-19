using Gw2Gizmos.Gw2Api.Contract.Home;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Home;

public interface IHomeCatsClient
    : IAllExpandableClient<HomeCat>,
        IBulkExpandableClient<HomeCat, int>,
        IPaginatedClient<HomeCat>;
