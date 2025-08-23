namespace Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

public class WizardsVaultObjective
{
    public int Id { get; set; }
    public string Title { get; set; }

    public WizardsVaultObjectiveTrack Track { get; set; }
    public int Acclaim { get; set; }
}
