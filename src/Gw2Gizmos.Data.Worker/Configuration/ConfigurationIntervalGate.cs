using System;
using Gw2Gizmos.Data.Worker.Features;
using Microsoft.Extensions.Configuration;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Default <see cref="IIntervalGate"/> for a standalone worker: reads <c>Worker:Intervals:&lt;Key&gt;</c> from
/// configuration (a <see cref="TimeSpan"/> string such as <c>"00:05:00"</c> or <c>"1.00:00:00"</c>), falling
/// back to the catalog default when unset or non-positive. When the desktop launches the worker it registers a
/// pipe-backed gate instead, so this is only used for standalone runs.
/// </summary>
public sealed class ConfigurationIntervalGate : IIntervalGate
{
    private readonly IConfiguration _configuration;

    public ConfigurationIntervalGate(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan GetInterval(string syncKey)
    {
        TimeSpan configured = _configuration.GetValue($"Worker:Intervals:{syncKey}", TimeSpan.Zero);
        return configured > TimeSpan.Zero ? configured : WorkerSyncs.DefaultInterval(syncKey);
    }
}
