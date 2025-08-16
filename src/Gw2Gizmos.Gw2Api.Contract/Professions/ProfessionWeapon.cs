namespace Gw2Gizmos.Gw2Api.Contract.Professions;

public class ProfessionWeapon
{
    public ProfessionWeaponFlag[] Flags { get; set; } = Array.Empty<ProfessionWeaponFlag>();
    public int? Specialization { get; set; }
    public ProfessionWeaponSkill[] Skills { get; set; } = Array.Empty<ProfessionWeaponSkill>();
}
