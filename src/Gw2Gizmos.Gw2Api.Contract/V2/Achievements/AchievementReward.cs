namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public class AchievementReward
{
    public AchievementRewardType Type { get; set; }
    public int? Count { get; set; }
    public int? Id { get; set; }
    public string? Region { get; set; }
}
