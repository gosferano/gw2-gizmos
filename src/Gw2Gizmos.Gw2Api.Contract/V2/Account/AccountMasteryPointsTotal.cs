namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class AccountMasteryPointsTotal
{
    /// <summary>
    /// The release the points belong to. Not a <see cref="MasteryPointRegion"/>: the masteries and the continents'
    /// mastery points name regions differently.
    /// </summary>
    public ReleaseName Region { get; set; }
    public int Spent { get; set; }
    public int Earned { get; set; }
}
