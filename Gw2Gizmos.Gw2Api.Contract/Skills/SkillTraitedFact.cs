namespace Gw2Gizmos.Gw2Api.Contract.Skills;

public class SkillTraitedFact : SkillFact
{
    public int RequiresTrait { get; set; }
    public int? Overrides { get; set; }
}
