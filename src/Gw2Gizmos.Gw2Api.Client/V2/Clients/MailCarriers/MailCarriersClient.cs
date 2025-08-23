using Gw2Gizmos.Gw2Api.Contract.V2.MailCarriers;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.MailCarriers;

public class MailCarriersClient : BaseBulkAllClient<MailCarrier, int>, IMailCarriersClient
{
    internal MailCarriersClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/mailcarriers";
}
