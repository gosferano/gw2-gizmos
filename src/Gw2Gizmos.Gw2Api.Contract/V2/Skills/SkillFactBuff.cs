namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public class SkillFactBuff : SkillFact
{
    public string Status { get; set; } = null!;
    public int? Duration { get; set; }
    public string? Description { get; set; }
    public int? ApplyCount { get; set; }
}
