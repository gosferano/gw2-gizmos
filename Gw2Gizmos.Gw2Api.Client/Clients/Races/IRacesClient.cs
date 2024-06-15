using Gw2Gizmos.Gw2Api.Contract.Races;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Races;

public interface IRacesClient : IAllExpandableClient<Race>, IBulkExpandableClient<Race, string>, IPaginatedClient<Race>;
