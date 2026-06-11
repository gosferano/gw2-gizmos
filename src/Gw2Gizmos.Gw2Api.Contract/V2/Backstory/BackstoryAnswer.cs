namespace Gw2Gizmos.Gw2Api.Contract.V2.Backstory;

public class BackstoryAnswer
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Journal { get; set; } = null!;
    public int Question { get; set; }
    public ProfessionName[] Professions { get; set; } = Array.Empty<ProfessionName>();
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
}
