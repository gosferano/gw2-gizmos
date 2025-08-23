using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Items;

public class ItemsClient : BaseBulkClient<Item, int>, IItemsClient
{
    internal ItemsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/items";
}
