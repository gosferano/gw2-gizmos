namespace Gw2Gizmos.Gw2Api.Contract.Professions;

public class ProfessionTraining
{
    public int Id { get; set; }
    public ProfessionTrainingCategory Category { get; set; }
    public string Name { get; set; }
    public ProfessionTrainingTrackStep[] Track { get; set; } = Array.Empty<ProfessionTrainingTrackStep>();
}
