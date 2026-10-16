using Avalonia.Media.Imaging;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Avalonia.Navigation;

/// <summary>
/// The few app services that lightweight controls (page chrome, icon images) reach statically because they are
/// created by XAML rather than resolved from DI. Set once at startup.
/// </summary>
public static class AppServices
{
    public static AvaloniaNavigationService? Navigation { get; internal set; }

    /// <summary>The item/currency icon cache behind <c>ItemImage</c> and <c>CurrencyImage</c>.</summary>
    public static IconProvider<Bitmap>? Icons { get; internal set; }
}
