using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public class TraitSkill
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public SkillFact[] Facts { get; set; } = Array.Empty<SkillFact>();
    public SkillFact[] TraitedFacts { get; set; } = Array.Empty<SkillFact>();
}
