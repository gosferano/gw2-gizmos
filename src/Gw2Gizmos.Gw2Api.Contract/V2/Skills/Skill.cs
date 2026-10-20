namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public sealed class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Icon { get; set; }
    public string ChatLink { get; set; } = null!;
    public SkillType Type { get; set; }
    public WeaponType? WeaponType { get; set; }
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

    /// <summary>The professions that can use the skill (none for monster and other non-profession skills).</summary>
    public ProfessionName[] Professions { get; set; } = Array.Empty<ProfessionName>();

    /// <summary>The specialization the skill belongs to, if any.</summary>
    public int? Specialization { get; set; }

    /// <summary>The skill's variants, e.g. an elementalist skill's version for each attunement.</summary>
    public SkillSubskill[] Subskills { get; set; } = Array.Empty<SkillSubskill>();

    /// <summary>The second attunement a weaver's dual-attunement weapon skill needs.</summary>
    public string? DualAttunement { get; set; }
}

public sealed class SkillSubskill
{
    public int Id { get; set; }
    public string? Attunement { get; set; }

    /// <summary>The form the variant is used in, e.g. "CelestialAvatar".</summary>
    public string? Form { get; set; }
}
