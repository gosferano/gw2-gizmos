using Gw2Gizmos.Gw2Api.Contract.V2.Maps;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Maps;

public interface IMapsClient : IAllExpandableClient<Map>, IBulkExpandableClient<Map, int>, IPaginatedClient<Map>;
