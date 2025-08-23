using Gw2Gizmos.Gw2Api.Contract.V2.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public interface IContinentsFloorsClient
    : IAllExpandableClient<ContinentFloor>,
        IBulkExpandableClient<ContinentFloor, int>,
        IPaginatedClient<ContinentFloor>
{
    IContinentsFloorsIdClient this[int floorId] { get; }
}
