namespace Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

public sealed class Achievement
{
    public int Id { get; set; }
    public string? Icon { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Requirement { get; set; } = null!;
    public string LockedText { get; set; } = null!;
    public AchievementType Type { get; set; }
    public AchievementFlag[] Flags { get; set; } = Array.Empty<AchievementFlag>();
    public AchievementTier[] Tiers { get; set; } = Array.Empty<AchievementTier>();
    public int[] Prerequisites { get; set; } = Array.Empty<int>();
    public AchievementReward[] Rewards { get; set; } = Array.Empty<AchievementReward>();
    public AchievementBit[] Bits { get; set; } = Array.Empty<AchievementBit>();
    public int? PointCap { get; set; }
}
