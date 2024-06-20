namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceTransactionsCurrentClient
{
    public ICommerceTransactionsCurrentBuysClient Buys { get; }
    public ICommerceTransactionsCurrentSellsClient Sells { get; }
}
