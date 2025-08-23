using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skills;

public interface ISkillsClient
    : IAllExpandableClient<Skill>,
        IBulkExpandableClient<Skill, int>,
        IPaginatedClient<Skill>;
