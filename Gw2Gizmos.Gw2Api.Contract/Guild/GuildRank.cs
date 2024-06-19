namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public class GuildRank
{
    public string Id { get; set; }
    public int Order { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public string Icon { get; set; }
}
