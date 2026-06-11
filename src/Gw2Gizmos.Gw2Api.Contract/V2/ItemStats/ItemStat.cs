namespace Gw2Gizmos.Gw2Api.Contract.V2.ItemStats;

public class ItemStat
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ItemStatAttribute[] Attributes { get; set; } = Array.Empty<ItemStatAttribute>();
}
