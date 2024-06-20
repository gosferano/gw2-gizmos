namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceTransactionsHistoryClient : IBlobClient<string[]>
{
    ICommerceTransactionsHistoryBuysClient Buys { get; }
    ICommerceTransactionsHistorySellsClient Sells { get; }
}
