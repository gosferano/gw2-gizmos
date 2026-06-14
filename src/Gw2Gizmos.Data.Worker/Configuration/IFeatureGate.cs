namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Tells the worker whether a feature (see <see cref="Features.WorkerFeatures"/>) is enabled. The source is
/// host-specific: the desktop's Settings page (pushed over the key pipe) when desktop-launched, or
/// <c>Worker:Features:&lt;Key&gt;</c> configuration for a standalone worker. Read live, so toggling a feature
/// takes effect without a restart.
/// </summary>
public interface IFeatureGate
{
    /// <summary>True when the feature should run; defaults to enabled for unknown/unset keys.</summary>
    bool IsEnabled(string featureKey);
}
