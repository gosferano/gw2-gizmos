using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public interface IContinentsFloorsRegionsMapsClient
    : IAllExpandableClient<ContinentFloorRegionMap>,
        IBulkExpandableClient<ContinentFloorRegionMap, int>,
        IPaginatedClient<ContinentFloorRegionMap>
{
    public IContinentsFloorsRegionsMapsIdClient this[int mapId] { get; }
}
