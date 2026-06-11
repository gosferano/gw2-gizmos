namespace Gw2Gizmos.Gw2Api.Contract.V2.Dungeons;

public class Dungeon
{
    public string Id { get; set; } = null!;
    public DungeonPath[] Paths { get; set; } = Array.Empty<DungeonPath>();
}
