namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceTransactionsHistoryClient : IBlobClient<string[]>
{
    ICommerceTransactionsHistoryBuysClient Buys { get; }
    ICommerceTransactionsHistorySellsClient Sells { get; }
}
