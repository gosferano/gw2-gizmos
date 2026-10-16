using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Platform.Windows;

public static class WindowsPlatformServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Windows platform services: toast notifications, "launch at Windows startup" (the HKCU Run key),
    /// DPAPI for the API keys at rest, and the startup work that sets the app's AUMID and binds the worker to a
    /// kill-on-close job.
    /// </summary>
    /// <param name="appFolderName">The app's data folder name ("Gw2Gizmos", or "Gw2Gizmos.Dev" for dev builds); names
    /// the Run-key value so dev and release builds register independently.</param>
    public static IServiceCollection AddGw2GizmosWindowsPlatform(this IServiceCollection services, string appFolderName)
    {
        services.AddSingleton<IToastService, WindowsToastService>();
        services.AddSingleton<IStartupRegistration>(new RunKeyStartupRegistration(appFolderName));
        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        // Velopack's Setup adds the app to Start, and "Apps & features" uninstalls it.
        services.AddSingleton<IAppInstallation, InstallerManagedInstallation>();
        services.AddHostedService<WindowsPlatformStartup>();
        return services;
    }
}
