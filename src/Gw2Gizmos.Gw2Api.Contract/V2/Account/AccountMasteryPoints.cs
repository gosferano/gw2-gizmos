namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class AccountMasteryPoints
{
    public AccountMasteryPointsTotal[] Totals { get; set; } = Array.Empty<AccountMasteryPointsTotal>();

    /// <summary>
    /// The ids of the mastery points the account has unlocked, as in the continents' mastery points.
    /// </summary>
    public int[] Unlocked { get; set; } = Array.Empty<int>();
}
