using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsMapsTasksClient
    : IAllExpandableClient<ContinentFloorRegionMapTask>,
        IBulkExpandableClient<ContinentFloorRegionMapTask, int>,
        IPaginatedClient<ContinentFloorRegionMapTask>;
