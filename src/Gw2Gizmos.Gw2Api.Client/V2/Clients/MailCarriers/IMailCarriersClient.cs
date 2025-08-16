using Gw2Gizmos.Gw2Api.Contract.MailCarriers;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.MailCarriers;

public interface IMailCarriersClient
    : IAllExpandableClient<MailCarrier>,
        IBulkExpandableClient<MailCarrier, int>,
        IPaginatedClient<MailCarrier>;
