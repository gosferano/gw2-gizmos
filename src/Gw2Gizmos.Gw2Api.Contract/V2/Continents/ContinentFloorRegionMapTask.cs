namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public class ContinentFloorRegionMapTask
{
    public string Objective { get; set; } = null!;
    public int Level { get; set; }
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public decimal[][] Bounds { get; set; } = Array.Empty<decimal[]>();
    public int Id { get; set; }
    public string ChatLink { get; set; } = null!;
}
