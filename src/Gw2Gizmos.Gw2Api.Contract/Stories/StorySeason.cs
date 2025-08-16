namespace Gw2Gizmos.Gw2Api.Contract.Stories;

public class StorySeason
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Order { get; set; }
    public int[] Stories { get; set; } = Array.Empty<int>();
}
