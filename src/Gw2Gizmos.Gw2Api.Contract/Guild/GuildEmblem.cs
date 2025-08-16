namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public class GuildEmblem
{
    public GuildEmblemComponent Background { get; set; }
    public GuildEmblemComponent Foreground { get; set; }
    public GuildEmblemFlag[] Flags { get; set; } = Array.Empty<GuildEmblemFlag>();
}
