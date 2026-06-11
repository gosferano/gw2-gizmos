namespace Gw2Gizmos.Gw2Api.Contract.V2.Colors;

public class Color
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] BaseRgb { get; set; } = Array.Empty<int>();
    public ColorMaterial Cloth { get; set; } = null!;
    public ColorMaterial Leather { get; set; } = null!;
    public ColorMaterial Metal { get; set; } = null!;
    public ColorMaterial Fur { get; set; } = null!;
    public int Item { get; set; }
    public ColorCategory[] Categories { get; set; } = Array.Empty<ColorCategory>();
}
