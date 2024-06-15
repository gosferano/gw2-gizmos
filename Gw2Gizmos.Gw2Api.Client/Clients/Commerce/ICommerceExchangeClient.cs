namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceExchangeClient : IBlobClient<string[]>
{
    public ICommerceExchangeCoinsClient Coins { get; }
}
