using Gw2Gizmos.Gw2Api.Contract.V2.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public interface IContinentsFloorsRegionsMapsSectorsClient
    : IAllExpandableClient<ContinentFloorRegionMapSector>,
        IBulkExpandableClient<ContinentFloorRegionMapSector, int>,
        IPaginatedClient<ContinentFloorRegionMapSector>;
