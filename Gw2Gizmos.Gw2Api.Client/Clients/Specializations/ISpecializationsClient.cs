using Gw2Gizmos.Gw2Api.Contract.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Specializations;

public interface ISpecializationsClient
    : IAllExpandableClient<Specialization>,
        IBulkExpandableClient<Specialization, int>,
        IPaginatedClient<Specialization>;
