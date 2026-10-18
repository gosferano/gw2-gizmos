using Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

public sealed class HomesteadDecorationsClient : BaseBulkClient<HomesteadDecoration, int>, IHomesteadDecorationsClient
{
    internal HomesteadDecorationsClient(HttpClient httpClient)
        : base(httpClient)
    {
        Categories = new HomesteadDecorationCategoriesClient(httpClient);
    }

    public IHomesteadDecorationCategoriesClient Categories { get; }

    protected override string UriPath => "/v2/homestead/decorations";
}
