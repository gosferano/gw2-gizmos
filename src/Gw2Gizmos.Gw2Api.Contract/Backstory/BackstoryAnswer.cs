namespace Gw2Gizmos.Gw2Api.Contract.Backstory;

public class BackstoryAnswer
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Journal { get; set; }
    public int Question { get; set; }
    public ProfessionName[] Professions { get; set; } = Array.Empty<ProfessionName>();
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
}
