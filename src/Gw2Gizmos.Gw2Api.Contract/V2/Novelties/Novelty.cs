namespace Gw2Gizmos.Gw2Api.Contract.V2.Novelties;

public class Novelty
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public NoveltySlot Slot { get; set; }
    public int[] UnlockItem { get; set; } = Array.Empty<int>();
}
