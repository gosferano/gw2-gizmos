namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public sealed class ContinentFloor
{
    public int[] TextureDims { get; set; } = Array.Empty<int>();
    public int[][] ClampedView { get; set; } = Array.Empty<int[]>();
    public Dictionary<int, ContinentFloorRegion> Regions { get; set; } = new();
}
