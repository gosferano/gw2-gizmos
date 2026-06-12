namespace Gw2Gizmos.Gw2Api.Contract.V2.Novelties;

public sealed class Novelty
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public NoveltySlot Slot { get; set; }
    public int[] UnlockItem { get; set; } = Array.Empty<int>();
}
