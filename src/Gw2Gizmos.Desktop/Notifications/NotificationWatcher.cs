using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Bridges notifications produced by *other* processes (e.g. the ingestion worker) into the app.
/// Polls the <c>Notifications</c> table for new rows whose <c>Source</c> isn't this app's, fires a toast
/// for each, and publishes them to the in-app feed. The app's own notifications are already toasted
/// and fed in-process by <see cref="DbNotifier"/> + the composite, so they are skipped here.
/// </summary>
public sealed class NotificationWatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationHub _hub;
    private int _lastSeenId;

    public NotificationWatcher(IServiceScopeFactory scopeFactory, NotificationHub hub)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start from the current tail so existing history isn't re-toasted on launch.
        _lastSeenId = await GetMaxIdAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }
            catch
            {
                // Transient DB contention etc. — try again next tick.
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

        List<Notification> batch = await dbContext
            .Notifications.Where(n => n.Id > _lastSeenId)
            .OrderBy(n => n.Id)
            .ToListAsync(stoppingToken);

        foreach (Notification notification in batch)
        {
            // Skip the app's own rows (handled in-process); surface everything else.
            if (notification.Source != DbNotifier.SourceName)
            {
                ToastService.Show(notification.Title, notification.Body);
                _hub.Publish(notification);
            }

            _lastSeenId = notification.Id;
        }
    }

    private async Task<int> GetMaxIdAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        return await dbContext.Notifications.AnyAsync(stoppingToken)
            ? await dbContext.Notifications.MaxAsync(n => n.Id, stoppingToken)
            : 0;
    }
}
