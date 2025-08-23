using Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;

public interface IAchievementGroupsClient
    : IAllExpandableClient<AchievementGroup>,
        IBulkExpandableClient<AchievementGroup, string>,
        IPaginatedClient<AchievementGroup> { }
