namespace Gw2Gizmos.Gw2Api.Contract.V2.Legends;

public sealed class Legend
{
    public string Id { get; set; } = null!;

    /// <summary>The legend's number, e.g. 1 for <c>Legend1</c>.</summary>
    public int Code { get; set; }
    public int Swap { get; set; }
    public int Heal { get; set; }
    public int Elite { get; set; }
    public int[] Utilities { get; set; } = Array.Empty<int>();
}
