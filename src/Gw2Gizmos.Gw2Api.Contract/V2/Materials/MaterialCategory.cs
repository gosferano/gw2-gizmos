namespace Gw2Gizmos.Gw2Api.Contract.V2.Materials;

public class MaterialCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int[] Items { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
}
