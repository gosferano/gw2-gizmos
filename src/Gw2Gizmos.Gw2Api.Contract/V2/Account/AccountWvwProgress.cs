namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

/// <summary>
/// The account's WvW team and rank, as part of <see cref="Account"/>. Not <see cref="AccountWvw"/>, which
/// /v2/account/wvw returns: that one has the WvW guild instead of the rank.
/// </summary>
public sealed class AccountWvwProgress
{
    public int TeamId { get; set; }
    public int Rank { get; set; }
}
