namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class Guild
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Tag { get; set; } = null!;
    public GuildEmblem Emblem { get; set; } = null!;
    public int Level { get; set; }
    public string Motd { get; set; } = null!;
    public int Influence { get; set; }
    public int Aetherium { get; set; }
    public int Favor { get; set; }
    public int MemberCount { get; set; }
    public int MemberCapacity { get; set; }
}
