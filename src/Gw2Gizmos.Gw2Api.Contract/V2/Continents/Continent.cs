namespace Gw2Gizmos.Gw2Api.Contract.V2.Continents;

public class Continent
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] ContinentDims { get; set; } = Array.Empty<int>();
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public int[] Floors { get; set; } = Array.Empty<int>();
}
