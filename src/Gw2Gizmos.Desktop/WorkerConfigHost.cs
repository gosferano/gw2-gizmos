using System;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Serves the desktop's live worker config — API keys + enabled features + per-sync intervals — to the worker
/// over a same-machine named pipe, so the cross-platform worker never reads the desktop's secret file or any OS
/// crypto. The pipe name is random per app run and passed to the worker at spawn; each client connection
/// receives a length-prefixed JSON <see cref="WorkerConfigPayload"/>.
/// </summary>
public sealed class WorkerConfigHost : BackgroundService
{
    private readonly string _pipeName;
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly FeatureSettingsStore _featureSettings;
    private readonly IntervalSettingsStore _intervalSettings;
    private readonly ILogger<WorkerConfigHost> _logger;

    public WorkerConfigHost(
        string pipeName,
        FileGw2ApiKeyStore keyStore,
        FeatureSettingsStore featureSettings,
        IntervalSettingsStore intervalSettings,
        ILogger<WorkerConfigHost> logger
    )
    {
        _pipeName = pipeName;
        _keyStore = keyStore;
        _featureSettings = featureSettings;
        _intervalSettings = intervalSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.Out,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                await server.WaitForConnectionAsync(stoppingToken);
                await WriteConfigAsync(server, stoppingToken);

                if (server.IsConnected)
                {
                    server.Disconnect();
                }
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker-config connection failed; awaiting the next client.");
            }
        }
    }

    private async Task WriteConfigAsync(NamedPipeServerStream server, CancellationToken stoppingToken)
    {
        var payload = new WorkerConfigPayload
        {
            Keys = _keyStore.GetApiKeys().ToArray(),
            EnabledFeatures = _featureSettings.EnabledKeys().ToArray(),
            Intervals = _intervalSettings.AllIntervals().ToDictionary(entry => entry.Key, entry => entry.Value),
        };

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);

        await server.WriteAsync(length, stoppingToken);
        await server.WriteAsync(bytes, stoppingToken);
        await server.FlushAsync(stoppingToken);
    }
}
