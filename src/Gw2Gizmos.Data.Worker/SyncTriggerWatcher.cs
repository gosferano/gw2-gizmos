using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Polls the <see cref="ISyncTriggerSource"/> on a short cadence and signals a sync's <see cref="SyncTrigger"/>
/// whenever its generation increases, so a feature the user just enabled (or a key they just added) syncs within
/// a few seconds instead of waiting for the next timer tick. Standalone workers use a no-op source, so this just
/// idles. The baseline is seeded from the first observation so a worker restart — which already runs every loop
/// once on startup — doesn't fire a redundant immediate run.
/// </summary>
public sealed class SyncTriggerWatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    private readonly ISyncTriggerSource _source;
    private readonly SyncTriggers _triggers;
    private readonly ILogger<SyncTriggerWatcher> _logger;

    public SyncTriggerWatcher(ISyncTriggerSource source, SyncTriggers triggers, ILogger<SyncTriggerWatcher> logger)
    {
        _source = source;
        _triggers = triggers;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Dictionary<string, long> lastActed =
            WorkerSyncs.All.ToDictionary(sync => sync.Key, sync => _source.GetGeneration(sync.Key));

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (WorkerSync sync in WorkerSyncs.All)
                {
                    long current = _source.GetGeneration(sync.Key);
                    if (current > lastActed[sync.Key])
                    {
                        lastActed[sync.Key] = current;
                        _triggers.Get(sync.Key).Signal();
                        _logger.LogInformation(
                            "Triggering immediate {Sync} sync (generation {Generation}).", sync.Key, current);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Worker is shutting down.
        }
    }
}
