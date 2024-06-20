namespace Gw2Gizmos.Gw2Api.Contract.Masteries;

public class Mastery
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Background { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public MasteryLevel[] Levels { get; set; } = Array.Empty<MasteryLevel>();
}
