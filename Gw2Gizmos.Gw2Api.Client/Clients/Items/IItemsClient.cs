using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Items;

public interface IItemsClient : IBulkExpandableClient<Item, int>, IPaginatedClient<Item> { }
