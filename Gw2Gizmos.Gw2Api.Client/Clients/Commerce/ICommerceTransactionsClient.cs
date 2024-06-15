namespace Gw2Gizmos.Gw2Api.Client.Clients.Commerce;

public interface ICommerceTransactionsClient : IBlobClient<string[]>
{
    public ICommerceTransactionsCurrentClient Current { get; }
    public ICommerceTransactionsHistoryClient History { get; }
}
