using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsClient
    : IAllExpandableClient<ContinentFloorRegion>,
        IBulkExpandableClient<ContinentFloorRegion, int>,
        IPaginatedClient<ContinentFloorRegion>
{
    public IContinentsFloorsRegionsIdClient this[int regionId] { get; }
}
