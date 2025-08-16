namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public class CommerceClient : ICommerceClient
{
    internal CommerceClient(HttpClient httpClient)
    {
        Delivery = new CommerceDeliveryClient(httpClient);
        Exchange = new CommerceExchangeClient(httpClient);
        Listings = new CommerceListingsClient(httpClient);
        Prices = new CommercePricesClient(httpClient);
        Transactions = new CommerceTransactionsClient(httpClient);
    }

    public ICommerceDeliveryClient Delivery { get; }
    public ICommerceExchangeClient Exchange { get; }
    public ICommerceListingsClient Listings { get; }
    public ICommercePricesClient Prices { get; }
    public ICommerceTransactionsClient Transactions { get; }
}
