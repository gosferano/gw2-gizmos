namespace Gw2Gizmos.Gw2Api.Contract.V2.Maps;

public class Map
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int DefaultFloor { get; set; }
    public MapType Type { get; set; }
    public int[] Floors { get; set; } = Array.Empty<int>();
    public int RegionId { get; set; }
    public string RegionName { get; set; } = null!;
    public int ContinentId { get; set; }
    public string ContinentName { get; set; } = null!;
    public int[][] MapRect { get; set; } = Array.Empty<int[]>();
    public int[][] ContinentRect { get; set; } = Array.Empty<int[]>();
}
