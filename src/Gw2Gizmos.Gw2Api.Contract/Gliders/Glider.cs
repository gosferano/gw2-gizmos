namespace Gw2Gizmos.Gw2Api.Contract.Gliders;

public class Glider
{
    public int Id { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int[] DefaultDyes { get; set; } = Array.Empty<int>();
}
