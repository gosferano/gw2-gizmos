using Avalonia.Media.Imaging;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary>Decodes downloaded GW2 icon files into Avalonia bitmaps for the shared <c>IconProvider</c>.</summary>
public static class AvaloniaIconDecoder
{
    /// <summary>Decode at the icons' native 64px so larger grid cells (10-per-row) stay crisp.</summary>
    private const int DecodePixelWidth = 64;

    /// <summary>An immutable bitmap, so one instance is safely shared by every row and node showing that icon.</summary>
    public static Bitmap Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Bitmap.DecodeToWidth(stream, DecodePixelWidth);
    }
}
