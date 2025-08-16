namespace Gw2Gizmos.Gw2Api.Contract.Outfits;

public class Outfit
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
}
