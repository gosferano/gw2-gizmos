namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public class AccountWizardsVaultObjective
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Track { get; set; }
    public int Acclaim { get; set; }
    public int ProgressCurrent { get; set; }
    public int ProgressComplete { get; set; }
    public bool Claimed { get; set; }
}
