namespace Gw2Gizmos.Gw2Api.Contract.V2.Gliders;

public sealed class Glider
{
    public int Id { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
    public string Icon { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int[] DefaultDyes { get; set; } = Array.Empty<int>();
}
