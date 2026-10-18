using Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public sealed class HomesteadDecorationCategoriesClient : BaseBulkAllClient<HomesteadDecorationCategory, int>, IHomesteadDecorationCategoriesClient
{
    internal HomesteadDecorationCategoriesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/homestead/decorations/categories";
}
