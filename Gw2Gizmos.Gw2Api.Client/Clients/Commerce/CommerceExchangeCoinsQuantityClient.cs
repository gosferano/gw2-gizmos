using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeCoinsQuantityClient : BaseClient, ICommerceExchangeCoinsQuantityClient
{
    private readonly int _quantity;

    internal CommerceExchangeCoinsQuantityClient(IGw2ApiClient apiClient, int quantity)
        : base(apiClient)
    {
        _quantity = quantity;
    }

    protected override string UriPath => "/v2/commerce/exchange/coins";

    public Task<CommerceExchange> GetBlob(CancellationToken cancellationToken = default)
    {
        return ApiClient.Get<CommerceExchange>($"{UriPath}?quantity={_quantity}", SchemaVersion, cancellationToken);
    }
}
