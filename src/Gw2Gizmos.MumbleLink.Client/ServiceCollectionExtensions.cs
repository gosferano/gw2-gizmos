using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.MumbleLink.Client;

/// <summary>DI registration for the MumbleLink reader.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMumbleLinkReader"/> as a singleton — one long-lived reader that keeps the
    /// shared-memory view open across reads. Inject <see cref="IMumbleLinkReader"/> and call
    /// <c>TryRead</c> on a poll. Windows-only.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddMumbleLink(this IServiceCollection services)
    {
        services.AddSingleton<IMumbleLinkReader>(_ => MumbleLinkReader.Create());
        return services;
    }
}
