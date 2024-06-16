using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementGroupsClient : BaseBulkAllClient<AchievementGroup, string>, IAchievementGroupsClient
{
    internal AchievementGroupsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/achievements/groups";
}
