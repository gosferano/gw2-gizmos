namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceExchangeClient : IBlobClient<string[]>
{
    public ICommerceExchangeCoinsClient Coins { get; }
    public ICommerceExchangeGemsClient Gems { get; }
}
