using Gw2Gizmos.Gw2Api.Contract.Minis;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Minis;

public interface IMinisClient : IAllExpandableClient<Mini>, IBulkExpandableClient<Mini, int>, IPaginatedClient<Mini>;
