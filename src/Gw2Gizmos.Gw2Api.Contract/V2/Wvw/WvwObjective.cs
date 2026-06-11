namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwObjective
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int SectorId { get; set; }
    public WvwObjectiveType Type { get; set; }
    public WvwMapType MapType { get; set; }
    public int MapId { get; set; }
    public int? UpgradeId { get; set; }
    public decimal[] Coord { get; set; } = Array.Empty<decimal>();
    public decimal[] LabelCoord { get; set; } = Array.Empty<decimal>();
    public string Marker { get; set; } = null!;
    public string ChatLink { get; set; } = null!;
}
