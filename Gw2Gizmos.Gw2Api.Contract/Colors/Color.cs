namespace Gw2Gizmos.Gw2Api.Contract.Colors;

public class Color
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int[] BaseRgb { get; set; } = Array.Empty<int>();
    public ColorMaterial Cloth { get; set; }
    public ColorMaterial Leather { get; set; }
    public ColorMaterial Metal { get; set; }
    public ColorMaterial Fur { get; set; }
    public int Item { get; set; }
    public ColorCategory[] Categories { get; set; } = Array.Empty<ColorCategory>();
}
