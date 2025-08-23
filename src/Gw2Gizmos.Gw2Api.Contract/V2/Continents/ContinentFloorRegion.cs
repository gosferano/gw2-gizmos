namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public class ContinentFloorRegion
{
    public string Name { get; set; }
    public int[] LabelCoord { get; set; } = Array.Empty<int>();
    public int[][] ContinentRect { get; set; } = Array.Empty<int[]>();
    public Dictionary<int, ContinentFloorRegionMap> Maps { get; set; } = new();
}
