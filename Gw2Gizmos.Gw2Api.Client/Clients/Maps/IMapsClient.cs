using Gw2Gizmos.Gw2Api.Contract.Maps;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Maps;

public interface IMapsClient : IAllExpandableClient<Map>, IBulkExpandableClient<Map, int>, IPaginatedClient<Map>;
