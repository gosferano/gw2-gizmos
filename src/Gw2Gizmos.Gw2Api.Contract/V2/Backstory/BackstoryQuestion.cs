namespace Gw2Gizmos.Gw2Api.Contract.V2.Backstory;

public class BackstoryQuestion
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string[] Answers { get; set; } = Array.Empty<string>();
    public int Order { get; set; }
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
    public ProfessionName[] Professions { get; set; } = Array.Empty<ProfessionName>();
}
