using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Features;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Fetches the desktop's live worker config — API keys, enabled features, and per-sync intervals — over a
/// same-machine named pipe (cross-platform via .NET pipes; no secret at rest, no OS crypto). Cached briefly so
/// it isn't a connection per call; changes in the desktop are picked up on the next refresh. Used only when the
/// desktop spawned the worker (pipe name passed in); standalone runs use the configuration/env providers.
/// </summary>
public sealed class IpcWorkerConfigProvider : IGw2ApiKeyProvider, IFeatureGate, IIntervalGate, ISyncTriggerSource
{
    // Short so a feature the user just enabled is seen — and its sync triggered — within a few seconds. The
    // generation map and the feature/key/interval values ride the same payload, so this also keeps the gate
    // consistent with the trigger (an enabled feature is visible by the time its sync is signalled).
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(3);
    private const int ConnectTimeoutMs = 2000;

    private readonly string _pipeName;
    private readonly ILogger<IpcWorkerConfigProvider> _logger;
    private readonly object _gate = new();
    private IReadOnlyList<string> _cachedKeys = Array.Empty<string>();
    private IReadOnlyList<string> _cachedEnabledFeatures = Array.Empty<string>();
    private IReadOnlyDictionary<string, TimeSpan> _cachedIntervals = new Dictionary<string, TimeSpan>();
    private IReadOnlyDictionary<string, long> _cachedGenerations = new Dictionary<string, long>();
    private DateTime _fetchedAtUtc = DateTime.MinValue;

    public IpcWorkerConfigProvider(string pipeName, ILogger<IpcWorkerConfigProvider> logger)
    {
        _pipeName = pipeName;
        _logger = logger;
    }

    public string? GetApiKey()
    {
        IReadOnlyList<string> keys = GetApiKeys();
        return keys.Count > 0 ? keys[0] : null;
    }

    public IReadOnlyList<string> GetApiKeys()
    {
        Refresh();
        lock (_gate)
        {
            return _cachedKeys;
        }
    }

    public bool IsEnabled(string featureKey)
    {
        Refresh();
        lock (_gate)
        {
            return _cachedEnabledFeatures.Contains(featureKey, StringComparer.OrdinalIgnoreCase);
        }
    }

    public TimeSpan GetInterval(string syncKey)
    {
        Refresh();
        lock (_gate)
        {
            if (_cachedIntervals.TryGetValue(syncKey, out TimeSpan interval) && interval > TimeSpan.Zero)
            {
                return interval;
            }
        }

        return WorkerSyncs.DefaultInterval(syncKey);
    }

    public long GetGeneration(string syncKey)
    {
        Refresh();
        lock (_gate)
        {
            return _cachedGenerations.TryGetValue(syncKey, out long generation) ? generation : 0;
        }
    }

    /// <summary>Refreshes the cached keys + features + intervals + generations from the desktop, no more often than the TTL.</summary>
    private void Refresh()
    {
        lock (_gate)
        {
            if (DateTime.UtcNow - _fetchedAtUtc < CacheTtl)
            {
                return;
            }
        }

        WorkerConfigPayload? payload = TryFetch();

        lock (_gate)
        {
            // On a fetch failure keep serving the last known values rather than dropping them mid-run.
            if (payload is not null)
            {
                _cachedKeys = (payload.Keys ?? [])
                    .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                    .ToArray();
                _cachedEnabledFeatures = payload.EnabledFeatures ?? Array.Empty<string>();
                _cachedIntervals = payload.Intervals ?? new Dictionary<string, TimeSpan>();
                _cachedGenerations = payload.SyncGenerations ?? new Dictionary<string, long>();
            }
            _fetchedAtUtc = DateTime.UtcNow;
        }
    }

    private WorkerConfigPayload? TryFetch()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.In);
            client.Connect(ConnectTimeoutMs);

            Span<byte> lengthBuffer = stackalloc byte[4];
            client.ReadExactly(lengthBuffer);
            int length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
            if (length is <= 0 or > 1_000_000)
            {
                return null;
            }

            byte[] bytes = new byte[length];
            client.ReadExactly(bytes);

            return JsonSerializer.Deserialize<WorkerConfigPayload>(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch the worker config from the desktop.");
            return null;
        }
    }
}
