namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;

public interface ICommerceClient
{
    ICommerceDeliveryClient Delivery { get; }
    ICommerceExchangeClient Exchange { get; }
    ICommerceListingsClient Listings { get; }
    ICommercePricesClient Prices { get; }
    ICommerceTransactionsClient Transactions { get; }
}
