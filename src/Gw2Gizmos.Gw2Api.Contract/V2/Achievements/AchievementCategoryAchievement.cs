namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public sealed class AchievementCategoryAchievement
{
    public int Id { get; set; }
    public AchievementCategoryAchievementFlag[] Flags { get; set; } = Array.Empty<AchievementCategoryAchievementFlag>();
    public int[] Level { get; set; } = Array.Empty<int>();
}
