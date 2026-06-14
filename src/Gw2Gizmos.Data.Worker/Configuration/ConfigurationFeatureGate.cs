using Microsoft.Extensions.Configuration;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Default <see cref="IFeatureGate"/> for a standalone worker: reads <c>Worker:Features:&lt;Key&gt;</c> from
/// configuration — one boolean per feature key. The shipped <c>appsettings.json</c> turns the features on, so a
/// feature that isn't configured at all defaults to <b>off</b> (no hardcoded always-on). When the desktop
/// launches the worker it registers an IPC-backed gate instead, so this is only used for standalone runs.
/// </summary>
public sealed class ConfigurationFeatureGate : IFeatureGate
{
    private readonly IConfiguration _configuration;

    public ConfigurationFeatureGate(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled(string featureKey) => _configuration.GetValue($"Worker:Features:{featureKey}", false);
}
