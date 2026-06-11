namespace Gw2Gizmos.Gw2Api.Contract.V2.Outfits;

public class Outfit
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
}
