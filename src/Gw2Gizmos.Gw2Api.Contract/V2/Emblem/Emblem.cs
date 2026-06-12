namespace Gw2Gizmos.Gw2Api.Contract.V2.Emblem;

public sealed class Emblem
{
    public int Id { get; set; }
    public string[] Layers { get; set; } = Array.Empty<string>();
}
