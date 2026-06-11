namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public class SkillFact
{
    public string Text { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public SkillFactType Type { get; set; }
    public int? RequiresTrait { get; set; }
    public int? Overrides { get; set; }
}
