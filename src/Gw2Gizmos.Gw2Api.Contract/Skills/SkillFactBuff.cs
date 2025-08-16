namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public class SkillFactBuff : SkillFact
{
    public int Duration { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public int ApplyCount { get; set; }
}
