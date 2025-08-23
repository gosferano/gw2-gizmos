namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public class Guild
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public GuildEmblem Emblem { get; set; }
    public int Level { get; set; }
    public string Motd { get; set; }
    public int Influence { get; set; }
    public int Aetherium { get; set; }
    public int Favor { get; set; }
    public int MemberCount { get; set; }
    public int MemberCapacity { get; set; }
}
