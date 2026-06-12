namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildRank
{
    public string Id { get; set; } = null!;
    public int Order { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public string Icon { get; set; } = null!;
}
