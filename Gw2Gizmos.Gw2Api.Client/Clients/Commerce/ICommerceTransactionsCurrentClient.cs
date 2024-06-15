namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceTransactionsCurrentClient
{
    public ICommerceTransactionsCurrentBuysClient Buys { get; }
    public ICommerceTransactionsCurrentSellsClient Sells { get; }
}
