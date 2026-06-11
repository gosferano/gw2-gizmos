namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public class ContinentFloorRegionMapPoi
{
    public string Name { get; set; } = null!;
    public ContinentFloorRegionMapPoiType Type { get; set; }
    public int Floor { get; set; }
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public int Id { get; set; }
    public string ChatLink { get; set; } = null!;

    /// <summary>
    /// Only specified if <see cref="Type"/> is <see cref="ContinentFloorRegionMapPoiType.Unlock"/>
    /// </summary>
    public string? Icon { get; set; }
}
