using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public interface IAchievementCategoriesClient
    : IAllExpandableClient<AchievementCategory>,
        IBulkExpandableClient<AchievementCategory, int>,
        IPaginatedClient<AchievementCategory> { }
