using Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skins;

public interface ISkinsClient : IBulkExpandableClient<Skin, int>, IPaginatedClient<Skin>;
