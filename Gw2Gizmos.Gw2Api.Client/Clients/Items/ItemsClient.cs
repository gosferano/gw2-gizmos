using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Items;

public class ItemsClient : BaseBulkClient<Item, int>, IItemsClient
{
    internal ItemsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/items";
}
