using Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;

public sealed class AchievementCategoriesClient : BaseBulkAllClient<AchievementCategory, int>, IAchievementCategoriesClient
{
    internal AchievementCategoriesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/achievements/categories";
}
