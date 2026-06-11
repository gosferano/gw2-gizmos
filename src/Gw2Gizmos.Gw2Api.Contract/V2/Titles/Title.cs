namespace Gw2Gizmos.Gw2Api.Contract.V2.Titles;

public class Title
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? Achievement { get; set; }
    public int[] Achievements { get; set; } = Array.Empty<int>();
    public int? ApRequired { get; set; }
}
