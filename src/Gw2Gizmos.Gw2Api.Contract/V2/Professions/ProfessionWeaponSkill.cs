namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public sealed class ProfessionWeaponSkill
{
    public int Id { get; set; }
    public SkillSlot Slot { get; set; }
    public string Offhand { get; set; } = null!;
    public string Attunement { get; set; } = null!;
    public string Source { get; set; } = null!;
}
