namespace Gw2Gizmos.Gw2Api.Contract.V2.Legends;

public class Legend
{
    public string Id { get; set; }
    public int Swap { get; set; }
    public int Heal { get; set; }
    public int Elite { get; set; }
    public int[] Utilities { get; set; } = Array.Empty<int>();
}
