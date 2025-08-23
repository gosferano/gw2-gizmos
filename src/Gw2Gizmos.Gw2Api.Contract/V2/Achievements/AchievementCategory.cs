namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public class AchievementCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public string Icon { get; set; }
    public AchievementCategoryAchievement[] Achievements { get; set; } = Array.Empty<AchievementCategoryAchievement>();
    public AchievementCategoryAchievement[] Tomorrow { get; set; } = Array.Empty<AchievementCategoryAchievement>();
}
