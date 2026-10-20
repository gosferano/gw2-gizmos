namespace Gw2Gizmos.Gw2Api.Contract.V2.Colors;

public sealed class Color
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] BaseRgb { get; set; } = Array.Empty<int>();
    // Each material can be missing: Hydra (id 1594) has no fur in the API, though every dye works on every slot in
    // game, so it's a gap in the data that could hit any material. Left null rather than an empty material, which
    // would read as black.
    public ColorMaterial? Cloth { get; set; }
    public ColorMaterial? Leather { get; set; }
    public ColorMaterial? Metal { get; set; }
    public ColorMaterial? Fur { get; set; }
    public int? Item { get; set; }
    public ColorCategory[] Categories { get; set; } = Array.Empty<ColorCategory>();
}
