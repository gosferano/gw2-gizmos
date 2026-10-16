using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.MumbleLink.Client;

/// <summary>DI registration for the MumbleLink reader.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMumbleLinkReader"/> as a singleton — one long-lived reader that keeps the
    /// shared-memory view open across reads (creating the mapping for the game to write into, if it's missing).
    /// Inject <see cref="IMumbleLinkReader"/> and call <c>TryRead</c> on a poll. Windows-only.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddMumbleLink(this IServiceCollection services)
    {
        services.AddSingleton<IMumbleLinkReader>(_ => MumbleLinkReader.Create());
        return services;
    }

    /// <summary>
    /// Registers <see cref="IMumbleLinkReader"/> as a singleton reading a file copy of the block at
    /// <paramref name="path"/> — for Linux, where the game runs under Wine/Proton and a bridge inside its prefix keeps
    /// that file in sync with the game's shared memory.
    /// </summary>
    public static IServiceCollection AddMumbleLinkFile(this IServiceCollection services, string path)
    {
        services.AddSingleton<IMumbleLinkReader>(_ => MumbleLinkReader.CreateForFile(path));
        return services;
    }
}
