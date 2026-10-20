using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public sealed class TraitSkill
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string ChatLink { get; set; } = null!;
    public SkillFlag[] Flags { get; set; } = Array.Empty<SkillFlag>();
    public SkillCategory[] Categories { get; set; } = Array.Empty<SkillCategory>();
    public SkillFact[] Facts { get; set; } = Array.Empty<SkillFact>();
    public SkillFact[] TraitedFacts { get; set; } = Array.Empty<SkillFact>();
}
