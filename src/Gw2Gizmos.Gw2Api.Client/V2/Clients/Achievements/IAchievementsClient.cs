using Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;

public interface IAchievementsClient : IBulkExpandableClient<Achievement, int>, IPaginatedClient<Achievement>
{
    public IAchievementCategoriesClient Categories { get; }
    public IAchievementGroupsClient Groups { get; }
}
