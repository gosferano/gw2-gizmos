namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildMember
{
    public string Name { get; set; }
    public string Rank { get; set; }
    public DateTimeOffset Joined { get; set; }
}
