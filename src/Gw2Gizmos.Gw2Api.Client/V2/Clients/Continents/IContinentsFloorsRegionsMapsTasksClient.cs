using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public interface IContinentsFloorsRegionsMapsTasksClient
    : IAllExpandableClient<ContinentFloorRegionMapTask>,
        IBulkExpandableClient<ContinentFloorRegionMapTask, int>,
        IPaginatedClient<ContinentFloorRegionMapTask>;
