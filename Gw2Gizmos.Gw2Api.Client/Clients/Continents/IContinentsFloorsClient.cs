using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsClient
    : IAllExpandableClient<ContinentFloor>,
        IBulkExpandableClient<ContinentFloor, int>,
        IPaginatedClient<ContinentFloor>
{
    IContinentsFloorsIdClient this[int floorId] { get; }
}
