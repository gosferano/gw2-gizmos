using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Cross-platform <see cref="IGw2ApiKeyProvider"/> that fetches the key from the desktop's key service over a
/// local named pipe (works on Windows/Linux/macOS via .NET pipes). The worker holds no secret at rest and uses
/// no OS crypto. The result is cached briefly so it isn't a connection per call; a key changed in the desktop
/// is picked up on the next refresh. Used only when the desktop spawned the worker (pipe name passed in);
/// standalone runs use the configuration/env provider instead.
/// </summary>
public sealed class IpcGw2ApiKeyProvider : IGw2ApiKeyProvider, IFeatureGate
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const int ConnectTimeoutMs = 2000;

    private readonly string _pipeName;
    private readonly ILogger<IpcGw2ApiKeyProvider> _logger;
    private readonly object _gate = new();
    private IReadOnlyList<string> _cachedKeys = Array.Empty<string>();
    private IReadOnlyList<string> _cachedEnabledFeatures = Array.Empty<string>();
    private DateTime _fetchedAtUtc = DateTime.MinValue;

    public IpcGw2ApiKeyProvider(string pipeName, ILogger<IpcGw2ApiKeyProvider> logger)
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

    /// <summary>Refreshes the cached keys + enabled features from the desktop, no more often than the TTL.</summary>
    private void Refresh()
    {
        lock (_gate)
        {
            if (DateTime.UtcNow - _fetchedAtUtc < CacheTtl)
            {
                return;
            }
        }

        KeyServiceResponse? response = TryFetch();

        lock (_gate)
        {
            // On a fetch failure keep serving the last known values rather than dropping them mid-run.
            if (response is not null)
            {
                _cachedKeys = (response.Keys ?? [])
                    .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                    .ToArray();
                _cachedEnabledFeatures = response.EnabledFeatures ?? Array.Empty<string>();
            }
            _fetchedAtUtc = DateTime.UtcNow;
        }
    }

    private KeyServiceResponse? TryFetch()
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

            byte[] payload = new byte[length];
            client.ReadExactly(payload);

            return JsonSerializer.Deserialize<KeyServiceResponse>(payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch the API keys from the desktop key service.");
            return null;
        }
    }
}
