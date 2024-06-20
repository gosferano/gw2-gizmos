using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Items;

public interface IItemsClient : IBulkExpandableClient<Item, int>, IPaginatedClient<Item> { }
