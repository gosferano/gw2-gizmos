using Gw2Gizmos.Gw2Api.Contract.V2.Minis;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Minis;

public interface IMinisClient : IAllExpandableClient<Mini>, IBulkExpandableClient<Mini, int>, IPaginatedClient<Mini>;
