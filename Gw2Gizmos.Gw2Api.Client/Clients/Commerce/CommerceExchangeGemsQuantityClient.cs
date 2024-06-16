using Gw2Gizmos.Gw2Api.Contract.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public class CommerceExchangeGemsQuantityClient : BaseClient, ICommerceExchangeGemsQuantityClient
{
    private readonly int _quantity;

    internal CommerceExchangeGemsQuantityClient(HttpClient httpClient, int quantity)
        : base(httpClient)
    {
        _quantity = quantity;
    }

    protected override string UriPath => "/v2/commerce/exchange/gems";

    public Task<CommerceExchange> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<CommerceExchange>($"{UriPath}?quantity={_quantity}", SchemaVersion, cancellationToken);
    }
}
