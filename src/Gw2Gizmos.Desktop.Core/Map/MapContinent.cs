namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A continent's floor as the official map tiles draw it, in continent units (the coordinates the API's map and
/// point-of-interest positions use). Tiles are 256px; at <see cref="NativeZoom"/> one tile pixel is one unit, and each
/// zoom level below halves the detail, so at zoom <c>z</c> a tile pixel spans <c>2^(NativeZoom - z)</c> units.
/// </summary>
/// <param name="Width">The continent's width in units (its <c>continent_dims</c>).</param>
/// <param name="Height">The continent's height in units.</param>
/// <param name="ViewMinX">The left edge of the area worth showing (the floor's <c>clamped_view</c>).</param>
/// <param name="ViewMinY">The top edge of the area worth showing; above it the tiles are empty.</param>
/// <param name="ViewMaxX">The right edge of the area worth showing.</param>
/// <param name="ViewMaxY">The bottom edge of the area worth showing.</param>
/// <param name="MaxTileZoom">
/// The highest zoom the API says the tile server has (<c>max_zoom</c>). For Tyria that's 8, though every zoom 8 tile
/// is currently a 404; the map then shows the zoom 7 tiles in their place.
/// </param>
public sealed record MapContinent(
    int Id,
    int Floor,
    double Width,
    double Height,
    double ViewMinX,
    double ViewMinY,
    double ViewMaxX,
    double ViewMaxY,
    int MaxTileZoom
)
{
    /// <summary>The zoom at which one tile pixel is one continent unit.</summary>
    public const int NativeZoom = 7;

    public const int TileSize = 256;

    /// <summary>
    /// Tyria's ground floor, from <c>/v2/continents/1</c> and <c>/v2/continents/1/floors/1</c>. The API lists Elona's
    /// maps only on floor 49, though the floor 1 tiles draw them.
    /// </summary>
    public static MapContinent Tyria { get; } =
        new(1, 1, 81920, 114688, 0, 9000, 81920, 111000, 8) { ExtraDataFloors = [49] };

    /// <summary>
    /// Further floors whose maps the tiles of <see cref="Floor"/> draw but the API lists elsewhere; their maps count
    /// where <see cref="Floor"/> lacks them.
    /// </summary>
    public IReadOnlyList<int> ExtraDataFloors { get; init; } = [];

    /// <summary>Continent units covered by one tile at <paramref name="zoom"/>.</summary>
    public static double UnitsPerTile(int zoom) => TileSize * Math.Pow(2, NativeZoom - zoom);

    /// <summary>The URL of one tile.</summary>
    public string TileUrl(int zoom, int x, int y) => $"https://tiles.guildwars2.com/{Id}/{Floor}/{zoom}/{x}/{y}.jpg";
}
