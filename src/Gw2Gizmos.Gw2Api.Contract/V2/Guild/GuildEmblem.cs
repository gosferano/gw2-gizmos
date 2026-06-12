namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildEmblem
{
    public GuildEmblemComponent Background { get; set; } = null!;
    public GuildEmblemComponent Foreground { get; set; } = null!;
    public GuildEmblemFlag[] Flags { get; set; } = Array.Empty<GuildEmblemFlag>();
}
