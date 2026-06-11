namespace Gw2Gizmos.Gw2Api.Contract.V2.Stories;

public class Story
{
    public int Id { get; set; }
    public string Season { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Timeline { get; set; } = null!;
    public int Level { get; set; }
    public int Order { get; set; }
    public StoryChapter[] Chapters { get; set; } = Array.Empty<StoryChapter>();
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
    public StoryFlag[] Flags { get; set; } = Array.Empty<StoryFlag>();
}
