namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public class AchievementGroup
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Order { get; set; }
    public int[] Categories { get; set; } = [];
}
