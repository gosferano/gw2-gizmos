namespace Gw2Gizmos.Gw2Api.Contract.V2.Currencies;

public sealed class Currency
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int Order { get; set; }
}
