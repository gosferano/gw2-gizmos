namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public sealed class ContinentFloorRegionMap
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int DefaultFloor { get; set; }
    public int[] LabelCoord { get; set; } = Array.Empty<int>();
    public int[][] MapRect { get; set; } = Array.Empty<int[]>();
    public int[][] ContinentRect { get; set; } = Array.Empty<int[]>();
    public Dictionary<int, ContinentFloorRegionMapPoi> PointsOfInterest { get; set; } = new();

    /// <summary>
    /// Only specified for the Ruins of Orr maps: the gods' temples, each either contested or uncontested.
    /// </summary>
    public ContinentFloorRegionMapGodShrine[]? GodShrines { get; set; }

    public Dictionary<int, ContinentFloorRegionMapTask> Tasks { get; set; } = new();
    public ContinentFloorRegionMapSkillChallenge[] SkillChallenges { get; set; } =
        Array.Empty<ContinentFloorRegionMapSkillChallenge>();
    public Dictionary<int, ContinentFloorRegionMapSector> Sectors { get; set; } = new();
    public ContinentFloorRegionMapAdventure[] Adventures { get; set; } =
        Array.Empty<ContinentFloorRegionMapAdventure>();
    public ContinentFloorRegionMapMasteryPoint[] MasteryPoints { get; set; } =
        Array.Empty<ContinentFloorRegionMapMasteryPoint>();
}
