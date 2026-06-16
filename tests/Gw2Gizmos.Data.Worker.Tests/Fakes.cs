using Gw2Gizmos.Data.Worker.Configuration;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>An <see cref="IGw2ApiKeyProvider"/> serving one fixed fake key, so the updater syncs exactly one account.</summary>
internal sealed class FakeApiKeyProvider : IGw2ApiKeyProvider
{
    private readonly string _key;

    public FakeApiKeyProvider(string key = "TEST-KEY") => _key = key;

    public string? GetApiKey() => _key;

    public IReadOnlyList<string> GetApiKeys() => new[] { _key };
}

/// <summary>An <see cref="IFeatureGate"/> that enables every feature, so all account sections run.</summary>
internal sealed class AllEnabledFeatureGate : IFeatureGate
{
    public bool IsEnabled(string featureKey) => true;
}

/// <summary>A <see cref="TimeProvider"/> whose "now" is settable, so tests can place each sync at a chosen instant.</summary>
internal sealed class MutableTimeProvider : TimeProvider
{
    // A fixed, arbitrary baseline so observation timestamps are deterministic.
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    /// <summary>Moves "now" forward, e.g. between two syncs so the second is strictly after the first.</summary>
    public void Advance(TimeSpan by) => Now += by;
}
