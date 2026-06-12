namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public sealed class Profession
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Code { get; set; }
    public string Icon { get; set; } = null!;
    public string IconBig { get; set; } = null!;
    public int[] Specializations { get; set; } = Array.Empty<int>();
    public ProfessionTraining[] Training { get; set; } = Array.Empty<ProfessionTraining>();
    public Dictionary<string, ProfessionWeapon> Weapons { get; set; } = new Dictionary<string, ProfessionWeapon>();
    public ProfessionFlag[] Flags { get; set; } = Array.Empty<ProfessionFlag>();
    public ProfessionSkill[] Skills { get; set; } = Array.Empty<ProfessionSkill>();
    public int[][] SkillsByPalette { get; set; } = Array.Empty<int[]>();
}
