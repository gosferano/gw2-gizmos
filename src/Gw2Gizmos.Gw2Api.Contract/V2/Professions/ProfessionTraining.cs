namespace Gw2Gizmos.Gw2Api.Contract.V2.Professions;

public class ProfessionTraining
{
    public int Id { get; set; }
    public ProfessionTrainingCategory Category { get; set; }
    public string Name { get; set; } = null!;
    public ProfessionTrainingTrackStep[] Track { get; set; } = Array.Empty<ProfessionTrainingTrackStep>();
}
