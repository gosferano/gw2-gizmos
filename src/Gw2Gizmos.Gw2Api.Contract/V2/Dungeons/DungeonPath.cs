namespace Gw2Gizmos.Gw2Api.Contract.V2.Dungeons;

public sealed class DungeonPath
{
    public string Id { get; set; } = null!;
    public DungeonPathType Type { get; set; }
}
