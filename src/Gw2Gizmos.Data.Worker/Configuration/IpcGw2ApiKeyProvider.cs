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
public sealed class IpcGw2ApiKeyProvider : IGw2ApiKeyProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private const int ConnectTimeoutMs = 2000;

    private readonly string _pipeName;
    private readonly ILogger<IpcGw2ApiKeyProvider> _logger;
    private readonly object _gate = new();
    private string? _cachedKey;
    private DateTime _fetchedAtUtc = DateTime.MinValue;

    public IpcGw2ApiKeyProvider(string pipeName, ILogger<IpcGw2ApiKeyProvider> logger)
    {
        _pipeName = pipeName;
        _logger = logger;
    }

    public string? GetApiKey()
    {
        lock (_gate)
        {
            if (DateTime.UtcNow - _fetchedAtUtc < CacheTtl)
            {
                return _cachedKey;
            }
        }

        string? key = TryFetch();

        lock (_gate)
        {
            // On a fetch failure keep serving the last known key rather than dropping it mid-run.
            if (key is not null)
            {
                _cachedKey = key;
            }
            _fetchedAtUtc = DateTime.UtcNow;
            return _cachedKey;
        }
    }

    private string? TryFetch()
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

            KeyServiceResponse? response = JsonSerializer.Deserialize<KeyServiceResponse>(payload);
            foreach (string candidate in response?.Keys ?? [])
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch the API key from the desktop key service.");
            return null;
        }
    }
}
