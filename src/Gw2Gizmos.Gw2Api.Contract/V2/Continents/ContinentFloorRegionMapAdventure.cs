namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public sealed class ContinentFloorRegionMapAdventure
{
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
