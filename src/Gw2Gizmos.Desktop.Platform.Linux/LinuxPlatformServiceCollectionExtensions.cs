using System.Runtime.Versioning;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

[SupportedOSPlatform("linux")]
public static class LinuxPlatformServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Linux platform services: <c>notify-send</c> notifications, an XDG autostart entry, the Secret
    /// Service keyring for API keys, and the launcher that runs the MumbleLink bridge next to GW2 under Wine/Proton.
    /// </summary>
    /// <param name="appFolderName">The app's data folder name ("Gw2Gizmos", or "Gw2Gizmos.Dev" for dev builds); names
    /// the autostart entry and the keyring item so dev and release builds don't clash.</param>
    /// <param name="mumbleLinkFilePath">Where the bridge mirrors MumbleLink; the worker must be given the same path.</param>
    public static IServiceCollection AddGw2GizmosLinuxPlatform(
        this IServiceCollection services, string appFolderName, string mumbleLinkFilePath)
    {
        services.AddSingleton<IToastService, NotifySendToastService>();
        services.AddSingleton<IStartupRegistration>(new XdgAutostartRegistration(appFolderName));
        services.AddSingleton<ISecretProtector>(sp => new SecretServiceProtector(
            appFolderName, sp.GetRequiredService<ILogger<SecretServiceProtector>>()));
        services.AddHostedService(sp => new ProtonMumbleLinkBridge(
            mumbleLinkFilePath,
            sp.GetRequiredService<FeatureSettingsStore>(),
            sp.GetRequiredService<ILogger<ProtonMumbleLinkBridge>>()));
        return services;
    }
}
