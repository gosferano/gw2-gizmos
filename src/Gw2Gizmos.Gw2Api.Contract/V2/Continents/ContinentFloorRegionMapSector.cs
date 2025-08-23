namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public class ContinentFloorRegionMapSector
{
    public string Name { get; set; }
    public int Level { get; set; }
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public decimal[][] Bounds { get; set; } = Array.Empty<decimal[]>();
    public int Id { get; set; }
    public string ChatLink { get; set; }
}
