namespace Gw2Gizmos.Gw2Api.Contract.V2.JadeBots;

public class JadeBot
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int UnlockItem { get; set; }
}
