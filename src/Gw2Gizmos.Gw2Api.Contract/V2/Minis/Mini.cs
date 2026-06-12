namespace Gw2Gizmos.Gw2Api.Contract.V2.Minis;

public sealed class Mini
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Unlock { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int Order { get; set; }
    public int ItemId { get; set; }
}
