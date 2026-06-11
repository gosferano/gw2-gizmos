namespace Gw2Gizmos.Gw2Api.Contract.V2.Stories;

public class StorySeason
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Order { get; set; }
    public int[] Stories { get; set; } = Array.Empty<int>();
}
