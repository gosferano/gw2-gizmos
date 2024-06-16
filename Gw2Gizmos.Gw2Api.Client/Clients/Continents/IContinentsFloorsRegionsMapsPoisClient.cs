using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsMapsPoisClient
    : IAllExpandableClient<ContinentFloorRegionMapPoi>,
        IBulkExpandableClient<ContinentFloorRegionMapPoi, int>,
        IPaginatedClient<ContinentFloorRegionMapPoi>;
