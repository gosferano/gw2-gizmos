namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public class ProfessionSkill
{
    public int Id { get; set; }
    public SkillSlot Slot { get; set; }
    public SkillType Type { get; set; }
    public string Source { get; set; }
}
