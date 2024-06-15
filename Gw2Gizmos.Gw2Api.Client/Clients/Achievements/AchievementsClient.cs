using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementsClient : BaseBulkClient<Achievement, int>, IAchievementsClient
{
    internal AchievementsClient(IGw2ApiClient apiClient)
        : base(apiClient)
    {
        Categories = new AchievementCategoriesClient(apiClient);
        Groups = new AchievementGroupsClient(apiClient);
    }

    public IAchievementCategoriesClient Categories { get; }
    public IAchievementGroupsClient Groups { get; }

    protected override string UriPath => "/v2/achievements";
}
