namespace Gw2Gizmos.Gw2Api.Contract.Continents;

public class ContinentFloorRegionMap
{
    public string Name { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int DefaultFloor { get; set; }
    public int[] LabelCoord { get; set; } = Array.Empty<int>();
    public int[][] MapRect { get; set; } = Array.Empty<int[]>();
    public int[][] ContinentRect { get; set; } = Array.Empty<int[]>();
    public Dictionary<int, ContinentFloorRegionMapPoi> PointsOfInterest { get; set; } = new();
    public Dictionary<int, ContinentFloorRegionMapTask> Tasks { get; set; } = new();
    public ContinentFloorRegionMapSkillChallenge[] SkillChallenges { get; set; } =
        Array.Empty<ContinentFloorRegionMapSkillChallenge>();
    public Dictionary<int, ContinentFloorRegionMapSector> Sectors { get; set; } = new();
    public ContinentFloorRegionMapAdventure[] Adventures { get; set; } =
        Array.Empty<ContinentFloorRegionMapAdventure>();
    public ContinentFloorRegionMapMasteryPoint[] MasteryPoints { get; set; } =
        Array.Empty<ContinentFloorRegionMapMasteryPoint>();
}
