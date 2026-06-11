namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public class AchievementCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Order { get; set; }
    public string Icon { get; set; } = null!;
    public AchievementCategoryAchievement[] Achievements { get; set; } = Array.Empty<AchievementCategoryAchievement>();
    public AchievementCategoryAchievement[] Tomorrow { get; set; } = Array.Empty<AchievementCategoryAchievement>();
}
