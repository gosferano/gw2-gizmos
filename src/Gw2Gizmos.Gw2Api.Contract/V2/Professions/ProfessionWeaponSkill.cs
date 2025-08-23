namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public class ProfessionWeaponSkill
{
    public int Id { get; set; }
    public SkillSlot Slot { get; set; }
    public string Offhand { get; set; }
    public string Attunement { get; set; }
    public string Source { get; set; }
}
