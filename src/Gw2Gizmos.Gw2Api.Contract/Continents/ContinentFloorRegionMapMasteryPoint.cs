namespace Gw2Gizmos.Gw2Api.Contract.Continents;

public class ContinentFloorRegionMapMasteryPoint
{
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public int Id { get; set; }
    public MasteryPointRegion Region { get; set; }
}
