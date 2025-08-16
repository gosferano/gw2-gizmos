namespace Gw2Gizmos.Gw2Api.Contract.Stories;

public class Story
{
    public int Id { get; set; }
    public string Season { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Timeline { get; set; }
    public int Level { get; set; }
    public int Order { get; set; }
    public StoryChapter[] Chapters { get; set; } = Array.Empty<StoryChapter>();
    public RaceName[] Races { get; set; } = Array.Empty<RaceName>();
    public StoryFlag[] Flags { get; set; } = Array.Empty<StoryFlag>();
}
