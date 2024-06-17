namespace Gw2Gizmos.Gw2Api.Contract.Finishers;

public class Finisher
{
    public int Id { get; set; }
    public string UnlockDetails { get; set; } = string.Empty;
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
}
