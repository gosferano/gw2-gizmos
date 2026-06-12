using System;
using Velopack;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Explicit entry point. Velopack must process install/update/uninstall hooks — and exit the process
/// for those — before WPF spins up, so <see cref="VelopackApp"/> runs here at the very top of Main
/// rather than in <c>App.OnStartup</c> (which runs after WPF has already initialized).
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
