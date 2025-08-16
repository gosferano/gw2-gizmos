using Gw2Gizmos.Gw2Api.Contract.Pets;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Pets;

public interface IPetsClient : IAllExpandableClient<Pet>, IBulkExpandableClient<Pet, int>, IPaginatedClient<Pet>;
