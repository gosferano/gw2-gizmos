namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildMember
{
    public string Name { get; set; } = null!;
    public string Rank { get; set; } = null!;
    public DateTimeOffset Joined { get; set; }
}
