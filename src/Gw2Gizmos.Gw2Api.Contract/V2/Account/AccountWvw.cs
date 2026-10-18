namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class AccountWvw
{
    /// <summary>
    /// The WvW team the account plays for. Matches list it among their <c>all_worlds</c>, and it works as the
    /// <c>world</c> to look a match up by.
    /// </summary>
    public int Team { get; set; }

    /// <summary>
    /// The guild the account represents in WvW, if it has picked one.
    /// </summary>
    public string? Guild { get; set; }
}
