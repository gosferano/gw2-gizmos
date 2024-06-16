namespace Gw2Gizmos.Gw2Api.Contract.Dungeons;

public class Dungeon
{
    public string Id { get; set; }
    public DungeonPath[] Paths { get; set; } = Array.Empty<DungeonPath>();
}
