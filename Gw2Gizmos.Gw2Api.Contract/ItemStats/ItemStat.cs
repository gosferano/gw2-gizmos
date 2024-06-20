namespace Gw2Gizmos.Gw2Api.Contract.ItemStats;

public class ItemStat
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ItemStatAttribute[] Attributes { get; set; } = Array.Empty<ItemStatAttribute>();
}
