namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public sealed class ContinentFloorRegionMapGodShrine
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string NameContested { get; set; } = null!;
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public int PoiId { get; set; }
    public string Icon { get; set; } = null!;
    public string IconContested { get; set; } = null!;
}
