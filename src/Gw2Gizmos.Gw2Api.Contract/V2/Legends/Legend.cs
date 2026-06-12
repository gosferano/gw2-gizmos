namespace Gw2Gizmos.Gw2Api.Contract.V2.Legends;

public sealed class Legend
{
    public string Id { get; set; } = null!;
    public int Swap { get; set; }
    public int Heal { get; set; }
    public int Elite { get; set; }
    public int[] Utilities { get; set; } = Array.Empty<int>();
}
