using Gw2Gizmos.Gw2Api.Contract.V2.Races;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Races;

public interface IRacesClient : IAllExpandableClient<Race>, IBulkExpandableClient<Race, string>, IPaginatedClient<Race>;
