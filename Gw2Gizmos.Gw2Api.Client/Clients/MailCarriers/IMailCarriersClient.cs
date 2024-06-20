using Gw2Gizmos.Gw2Api.Contract.MailCarriers;

namespace Gw2Gizmos.Gw2Api.Client.Clients.MailCarriers;

public interface IMailCarriersClient
    : IAllExpandableClient<MailCarrier>,
        IBulkExpandableClient<MailCarrier, int>,
        IPaginatedClient<MailCarrier>;
