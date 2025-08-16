namespace Gw2Gizmos.Gw2Api.Contract.Achievements;

public class AchievementGroup
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public int[] Categories { get; set; }
}
