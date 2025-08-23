using Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;

public class AchievementsClient : BaseBulkClient<Achievement, int>, IAchievementsClient
{
    internal AchievementsClient(HttpClient httpClient)
        : base(httpClient)
    {
        Categories = new AchievementCategoriesClient(httpClient);
        Groups = new AchievementGroupsClient(httpClient);
    }

    public IAchievementCategoriesClient Categories { get; }
    public IAchievementGroupsClient Groups { get; }

    protected override string UriPath => "/v2/achievements";
}
