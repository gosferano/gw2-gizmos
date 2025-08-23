namespace Gw2Gizmos.Gw2Api.Contract.V2.Colors;

public class ColorMaterial
{
    public decimal Brightness { get; set; }
    public decimal Contrast { get; set; }
    public decimal Hue { get; set; }
    public decimal Saturation { get; set; }
    public decimal Lightness { get; set; }
    public int[] Rgb { get; set; } = Array.Empty<int>();
}
