using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary>Decodes downloaded GW2 icon files into WPF images for the shared <c>IconProvider</c>.</summary>
public static class WpfIconDecoder
{
    /// <summary>Decode at the icons' native 64px so larger grid cells (10-per-row) stay crisp.</summary>
    private const int DecodePixelWidth = 64;

    /// <summary>A frozen bitmap, so one instance is safely shared by every row and tree node showing that icon.</summary>
    public static ImageSource Decode(byte[] bytes)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = DecodePixelWidth;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
