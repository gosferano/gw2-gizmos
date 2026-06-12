using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public sealed class CommerceExchangeGemsQuantityClient : BaseClient, ICommerceExchangeGemsQuantityClient
{
    private readonly int _quantity;

    internal CommerceExchangeGemsQuantityClient(HttpClient httpClient, int quantity)
        : base(httpClient)
    {
        _quantity = quantity;
    }

    protected override string UriPath => "/v2/commerce/exchange/gems";

    public Task<Result<CommerceExchange, Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<CommerceExchange>($"{UriPath}?quantity={_quantity}", SchemaVersion, cancellationToken);
    }
}
