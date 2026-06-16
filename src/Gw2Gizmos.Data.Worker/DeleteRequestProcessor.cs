using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Updaters;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Runs the desktop's queued data-delete requests (see <see cref="DeleteRequest"/>). Polls the source a few times
/// a minute and executes any request id it hasn't processed yet, serialized through the <see cref="AccountSyncGate"/>
/// so a delete never races the account sync's writes. Ids are monotonic and the queue is in-memory on the desktop,
/// so a processed request can't replay after a restart.
/// </summary>
public sealed class DeleteRequestProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeleteRequestSource _source;
    private readonly AccountSyncGate _accountSyncGate;
    private readonly ILogger<DeleteRequestProcessor> _logger;
    private long _lastProcessedId;

    public DeleteRequestProcessor(
        IServiceScopeFactory scopeFactory,
        IDeleteRequestSource source,
        AccountSyncGate accountSyncGate,
        ILogger<DeleteRequestProcessor> logger
    )
    {
        _scopeFactory = scopeFactory;
        _source = source;
        _accountSyncGate = accountSyncGate;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Processing delete requests failed; continuing.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessPendingAsync(CancellationToken stoppingToken)
    {
        List<DeleteRequest> pending = _source.GetDeleteRequests()
            .Where(request => request.Id > _lastProcessedId)
            .OrderBy(request => request.Id)
            .ToList();

        foreach (DeleteRequest request in pending)
        {
            await _accountSyncGate.RunExclusivelyAsync(
                async () =>
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var deleter = scope.ServiceProvider.GetRequiredService<AccountDataDeleter>();
                    await deleter.DeleteAsync(request.TypeKey, request.AccountId, stoppingToken);
                },
                stoppingToken
            );
            _lastProcessedId = Math.Max(_lastProcessedId, request.Id);
        }
    }
}
