namespace Gw2Gizmos.Gw2Api.Contract.Account;

public class AccountWizardsVaultCategory
{
    public int MetaProgressCurrent { get; set; }
    public int MetaProgressComplete { get; set; }
    public int MetaRewardItemId { get; set; }
    public int MetaRewardAstral { get; set; }
    public bool MetaRewardClaimed { get; set; }
    public AccountWizardsVaultObjective[] Objectives { get; set; } = Array.Empty<AccountWizardsVaultObjective>();
}
