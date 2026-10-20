namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public class SkillFactBuff : SkillFact
{
    /// <summary>
    /// The buff's name, e.g. "Might". Missing on a broken record or two, e.g. Trail of Knives' traited buff fact, which
    /// has a duration and stack count but no buff.
    /// </summary>
    public string? Status { get; set; }
    public int? Duration { get; set; }
    public string? Description { get; set; }
    public int? ApplyCount { get; set; }
}
