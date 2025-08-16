namespace Gw2Gizmos.Gw2Api.Contract.Specializations;

public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Profession { get; set; }
    public bool Elite { get; set; }
    public int[] MinorTraits { get; set; } = Array.Empty<int>();
    public int[] MajorTraits { get; set; } = Array.Empty<int>();
    public string Icon { get; set; }
    public string Background { get; set; }
}
