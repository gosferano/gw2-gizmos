namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public sealed class ContinentFloorRegionMapMasteryPoint
{
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public int Id { get; set; }
    public MasteryPointRegion Region { get; set; }
}
