using Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;

public interface IAchievementCategoriesClient
    : IAllExpandableClient<AchievementCategory>,
        IBulkExpandableClient<AchievementCategory, int>,
        IPaginatedClient<AchievementCategory> { }
