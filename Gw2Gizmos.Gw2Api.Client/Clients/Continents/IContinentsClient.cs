using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsClient
    : IAllExpandableClient<Continent>,
        IBulkExpandableClient<Continent, int>,
        IPaginatedClient<Continent>
{
    public IContinentsIdClient this[int continentId] { get; }
}
