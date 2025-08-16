namespace Gw2Gizmos.Gw2Api.Contract.Account;

public class Account
{
    public string Id { get; set; }
    public string Name { get; set; }
    public long Age { get; set; }
    public int World { get; set; }
    public string[] Guilds { get; set; } = Array.Empty<string>();
    public string[] GuildLeader { get; set; } = Array.Empty<string>();
    public DateTimeOffset Created { get; set; }
    public AccountAccess[] Access { get; set; } = Array.Empty<AccountAccess>();
    public bool Commander { get; set; }
    public int FractalLevel { get; set; }
    public int DailyAp { get; set; }
    public int MonthlyAp { get; set; }
    public int WvwRank { get; set; }
}
