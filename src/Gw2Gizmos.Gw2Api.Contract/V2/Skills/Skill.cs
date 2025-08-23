namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string ChatLink { get; set; }
    public SkillType Type { get; set; }
    public WeaponType? WeaponType { get; set; }
    public ProfessionName Profession { get; set; }
    public SkillSlot? Slot { get; set; }
    public SkillFact[] Facts { get; set; } = Array.Empty<SkillFact>();
    public SkillFact[] TraitedFacts { get; set; } = Array.Empty<SkillFact>();
    public SkillCategory[] Categories { get; set; } = Array.Empty<SkillCategory>();
    public string? Attunement { get; set; }
    public int? Cost { get; set; }
    public WeaponType? DualWield { get; set; }
    public int? FlipSkill { get; set; }
    public int? Initiative { get; set; }
    public int? NextChain { get; set; }
    public int? PrevChain { get; set; }
    public int[] TransformSkills { get; set; } = Array.Empty<int>();
    public int[] BundleSkills { get; set; } = Array.Empty<int>();
    public int? ToolbeltSkill { get; set; }
    public SkillFlag[] Flags { get; set; } = Array.Empty<SkillFlag>();
}
