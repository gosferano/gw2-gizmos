using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementsClient : BaseBulkClient<Achievement, int>, IAchievementsClient
{
    internal AchievementsClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Categories = new AchievementCategoriesClient(apiClient);
    }

    public IAchievementCategoriesClient Categories { get; }

    protected override string UriPath => "/v2/achievements";
}
