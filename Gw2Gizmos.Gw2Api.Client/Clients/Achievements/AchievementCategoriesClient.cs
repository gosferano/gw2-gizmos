using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementCategoriesClient : BaseBulkAllClient<AchievementCategory, int>, IAchievementCategoriesClient
{
    internal AchievementCategoriesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/achievements/categories";
}
