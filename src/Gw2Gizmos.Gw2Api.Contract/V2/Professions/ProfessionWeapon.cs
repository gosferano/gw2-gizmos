namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public sealed class ProfessionWeapon
{
    public ProfessionWeaponFlag[] Flags { get; set; } = Array.Empty<ProfessionWeaponFlag>();
    public int? Specialization { get; set; }
    public ProfessionWeaponSkill[] Skills { get; set; } = Array.Empty<ProfessionWeaponSkill>();
}
