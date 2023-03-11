using Gw2Gizmos.BuildMachine.Model.Skills;

namespace Gw2Gizmos.BuildMachine.Model.Legends;

public class Legend
{
    public string Id { get; }
    public int Code { get; }
    public Skill SwapSkill { get; }
    public Skill HealSkill { get; }
    public Skill EliteSkill { get; }
    public Skill[] UtilitySkills { get; }

    public Legend(
        string id,
        int code,
        Skill swapSkill,
        Skill healSkill,
        Skill eliteSkill,
        Skill[] utilitySkills)
    {
        Id = id;
        Code = code;
        SwapSkill = swapSkill;
        HealSkill = healSkill;
        EliteSkill = eliteSkill;
        UtilitySkills = utilitySkills;
    }
}