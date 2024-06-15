using Gw2Gizmos.Gw2Api.Contract.Achievements;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Achievements;

public interface IAchievementsClient : IBulkExpandableClient<Achievement, int>, IPaginatedClient<Achievement>
{
    public IAchievementCategoriesClient Categories { get; }
}
