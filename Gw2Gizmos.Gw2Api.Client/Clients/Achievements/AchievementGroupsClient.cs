using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public class AchievementGroupsClient : BaseBulkAllClient<AchievementGroup, string>, IAchievementGroupsClient
{
    internal AchievementGroupsClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    protected override string UriPath => "/v2/achievements/groups";
}
