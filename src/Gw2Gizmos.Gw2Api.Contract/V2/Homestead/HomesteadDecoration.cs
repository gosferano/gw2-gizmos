namespace Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

public sealed class HomesteadDecoration
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>
    /// Empty for some decorations.
    /// </summary>
    public string Description { get; set; } = null!;

    public int[] Categories { get; set; } = Array.Empty<int>();
    public int MaxCount { get; set; }
    public string Icon { get; set; } = null!;
}
