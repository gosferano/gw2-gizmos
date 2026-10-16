using Avalonia;
using Velopack;

namespace Gw2Gizmos.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must process install/update/uninstall hooks — and exit the process for those — before the UI
        // starts; it also has to run before any UpdateManager is created (the update check).
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Also used by the XAML previewer, so keep it free of side effects.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Draw popups (dropdowns, flyouts, tooltips) inside the window rather than as separate X11 windows: window
            // managers may treat those as ordinary floating windows and apply rules to them (e.g. a Hyprland "centre
            // floating windows" rule put every dropdown in the middle of the screen).
            .With(new X11PlatformOptions { OverlayPopups = true })
            .LogToTrace();
}
