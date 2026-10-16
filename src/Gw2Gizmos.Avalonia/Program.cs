using Avalonia;
using Velopack;

namespace Gw2Gizmos.Avalonia;

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
            .LogToTrace();
}
