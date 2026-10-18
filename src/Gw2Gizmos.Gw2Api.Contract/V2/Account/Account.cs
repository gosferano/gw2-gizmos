namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class Account
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public long Age { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public int World { get; set; }
    public string[] Guilds { get; set; } = Array.Empty<string>();
    public string[] GuildLeader { get; set; } = Array.Empty<string>();
    public DateTimeOffset Created { get; set; }
    public AccountAccess[] Access { get; set; } = Array.Empty<AccountAccess>();
    public bool Commander { get; set; }
    public int FractalLevel { get; set; }
    public int DailyAp { get; set; }
    public int MonthlyAp { get; set; }
    public AccountWvwProgress Wvw { get; set; } = null!;

    /// <summary>
    /// Only specified if the token has the <c>builds</c> permission.
    /// </summary>
    public int? BuildStorageSlots { get; set; }
}
