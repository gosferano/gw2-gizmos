using Gw2Gizmos.Gw2Api.Contract.V2.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Specializations;

public interface ISpecializationsClient
    : IAllExpandableClient<Specialization>,
        IBulkExpandableClient<Specialization, int>,
        IPaginatedClient<Specialization>;
