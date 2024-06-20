namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceTransactionsClient : IBlobClient<string[]>
{
    public ICommerceTransactionsCurrentClient Current { get; }
    public ICommerceTransactionsHistoryClient History { get; }
}
