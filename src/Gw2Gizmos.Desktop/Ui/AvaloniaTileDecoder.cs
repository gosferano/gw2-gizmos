using Avalonia.Media.Imaging;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary>Decodes downloaded map tiles into Avalonia bitmaps for the shared <c>MapTileCache</c>.</summary>
public static class AvaloniaTileDecoder
{
    /// <summary>At full size (256px): the map scales tiles itself as it zooms.</summary>
    public static Bitmap Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }
}
