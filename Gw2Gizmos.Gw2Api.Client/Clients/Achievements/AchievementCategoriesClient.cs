using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementCategoriesClient : BaseBulkAllClient<AchievementCategory, int>, IAchievementCategoriesClient
{
    internal AchievementCategoriesClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/achievements/categories";
}
