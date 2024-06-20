namespace Gw2Gizmos.Gw2Api.Contract.Professions;

public class Profession
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Code { get; set; }
    public string Icon { get; set; }
    public string IconBig { get; set; }
    public int[] Specializations { get; set; } = Array.Empty<int>();
    public ProfessionTraining[] Training { get; set; } = Array.Empty<ProfessionTraining>();
    public Dictionary<string, ProfessionWeapon> Weapons { get; set; } = new Dictionary<string, ProfessionWeapon>();
    public ProfessionFlag[] Flags { get; set; } = Array.Empty<ProfessionFlag>();
    public ProfessionSkill[] Skills { get; set; } = Array.Empty<ProfessionSkill>();
    public int[][] SkillsByPalette { get; set; } = Array.Empty<int[]>();
}
