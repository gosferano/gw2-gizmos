namespace Gw2Gizmos.Gw2Api.Contract.Continents;

public class ContinentFloor
{
    public int[] TextureDims { get; set; } = Array.Empty<int>();
    public int[][] ClampedView { get; set; } = Array.Empty<int[]>();
    public Dictionary<int, ContinentFloorRegion> Regions { get; set; } = new();
}
