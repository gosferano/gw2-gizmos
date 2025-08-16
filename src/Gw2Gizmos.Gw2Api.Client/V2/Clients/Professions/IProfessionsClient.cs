using Gw2Gizmos.Gw2Api.Contract.Professions;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Professions;

public interface IProfessionsClient
    : IAllExpandableClient<Profession>,
        IBulkExpandableClient<Profession, string>,
        IPaginatedClient<Profession>;
