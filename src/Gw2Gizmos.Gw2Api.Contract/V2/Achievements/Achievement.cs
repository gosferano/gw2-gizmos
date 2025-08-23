namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public class Achievement
{
    public int Id { get; set; }
    public string? Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Requirement { get; set; }
    public string LockedText { get; set; }
    public AchievementType Type { get; set; }
    public AchievementFlag[] Flags { get; set; } = Array.Empty<AchievementFlag>();
    public AchievementTier[] Tiers { get; set; } = Array.Empty<AchievementTier>();
    public int[] Prerequisites { get; set; } = Array.Empty<int>();
    public AchievementReward[] Rewards { get; set; } = Array.Empty<AchievementReward>();
    public AchievementBit[] Bits { get; set; } = Array.Empty<AchievementBit>();
    public int? PointCap { get; set; }
}
