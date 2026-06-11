namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class GuildTeamMember
{
    public string Name { get; set; } = null!;
    public GuildTeamMemberRole Role { get; set; }
}
