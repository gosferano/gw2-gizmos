using Avalonia.Media.Imaging;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Navigation;

/// <summary>
/// The few app services that lightweight controls (page chrome, icon images) reach statically because they are
/// created by XAML rather than resolved from DI. Set once at startup.
/// </summary>
public static class AppServices
{
    public static AvaloniaNavigationService? Navigation { get; internal set; }

    /// <summary>The item/currency icon cache behind <c>ItemImage</c> and <c>IconImage</c>.</summary>
    public static IconProvider<Bitmap>? Icons { get; internal set; }

    /// <summary>The map tile cache behind <c>MapView</c>.</summary>
    public static MapTileCache<Bitmap>? Tiles { get; internal set; }

    /// <summary>Item rarities, behind the rarity colours of item names and <c>ItemImage</c> borders.</summary>
    public static ItemRarities? Rarities { get; internal set; }
}
