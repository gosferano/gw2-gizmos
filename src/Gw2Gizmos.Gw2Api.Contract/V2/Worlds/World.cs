namespace Gw2Gizmos.Gw2Api.Contract.V2.Worlds;

public class World
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public WorldPopulation Population { get; set; }
}
