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
/// Serves the current GW2 API key(s) to the worker over a same-machine named pipe, so the cross-platform
/// worker never reads the desktop's secret file or any OS crypto. The pipe name is random per app run and
/// passed to the worker at spawn; each client connection receives a length-prefixed JSON
/// <see cref="KeyServiceResponse"/>. The payload is a list so multiple accounts can be served later.
/// </summary>
public sealed class KeyServiceHost : BackgroundService
{
    private readonly string _pipeName;
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly FeatureSettingsStore _featureSettings;
    private readonly ILogger<KeyServiceHost> _logger;

    public KeyServiceHost(
        string pipeName,
        FileGw2ApiKeyStore keyStore,
        FeatureSettingsStore featureSettings,
        ILogger<KeyServiceHost> logger
    )
    {
        _pipeName = pipeName;
        _keyStore = keyStore;
        _featureSettings = featureSettings;
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
                await WriteKeysAsync(server, stoppingToken);

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
                _logger.LogWarning(ex, "Key service connection failed; awaiting the next client.");
            }
        }
    }

    private async Task WriteKeysAsync(NamedPipeServerStream server, CancellationToken stoppingToken)
    {
        var response = new KeyServiceResponse
        {
            Keys = _keyStore.GetApiKeys().ToArray(),
            EnabledFeatures = _featureSettings.EnabledKeys().ToArray(),
        };

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(response);
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, payload.Length);

        await server.WriteAsync(length, stoppingToken);
        await server.WriteAsync(payload, stoppingToken);
        await server.FlushAsync(stoppingToken);
    }
}
