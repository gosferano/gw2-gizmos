namespace Gw2Gizmos.Gw2Api.Contract.Backstory;

public class BackstoryQuestion
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string[] Answers { get; set; } = Array.Empty<string>();
    public int Order { get; set; }
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
    public ProfessionName[] Professions { get; set; } = Array.Empty<ProfessionName>();
}
