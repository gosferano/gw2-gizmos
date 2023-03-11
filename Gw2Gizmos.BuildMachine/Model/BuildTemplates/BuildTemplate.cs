using Gw2Gizmos.BuildMachine.Model.Skills;

namespace Gw2Gizmos.BuildMachine.Model.BuildTemplates;

public class BuildTemplate
{
    public Skill HealSkill { get; }
    public Skill[] UtilitySkills { get; }
    public Skill EliteSkill { get; }
}