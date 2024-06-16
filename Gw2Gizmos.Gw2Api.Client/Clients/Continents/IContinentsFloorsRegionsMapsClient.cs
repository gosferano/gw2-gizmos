using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsMapsClient
    : IAllExpandableClient<ContinentFloorRegionMap>,
        IBulkExpandableClient<ContinentFloorRegionMap, int>,
        IPaginatedClient<ContinentFloorRegionMap>
{
    public IContinentsFloorsRegionsMapsIdClient this[int mapId] { get; }
}
