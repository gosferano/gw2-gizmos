using Gw2Gizmos.Gw2Api.Contract.V2.Raids;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Raids;

public interface IRaidsClient : IAllExpandableClient<Raid>, IBulkExpandableClient<Raid, string>, IPaginatedClient<Raid>;
