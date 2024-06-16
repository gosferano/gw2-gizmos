using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsMapsSectorsClient
    : IAllExpandableClient<ContinentFloorRegionMapSector>,
        IBulkExpandableClient<ContinentFloorRegionMapSector, int>,
        IPaginatedClient<ContinentFloorRegionMapSector>;
