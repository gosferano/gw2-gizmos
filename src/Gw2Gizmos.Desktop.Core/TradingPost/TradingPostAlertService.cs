using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Notifies when one of the selected account's open trading-post orders gets undercut or outbid. The worker refreshes
/// the orders (and the competing prices) in the account sync; this re-reads them periodically and notifies each order
/// once as it gets beaten (<see cref="TradingPostAlerts"/>). The notified set is kept in a per-user file, so an app
/// restart doesn't repeat them. Runs in the desktop process because that owns the OS notifications.
/// </summary>
public sealed class TradingPostAlertService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly AccountReader _reader;
    private readonly SelectedAccountService _selected;
    private readonly INotifier _notifier;
    private readonly ILogger<TradingPostAlertService> _logger;
    private readonly string _path;

    public TradingPostAlertService(
        AccountReader reader,
        SelectedAccountService selected,
        INotifier notifier,
        AppPaths paths,
        ILogger<TradingPostAlertService> logger)
    {
        _reader = reader;
        _selected = selected;
        _notifier = notifier;
        _logger = logger;
        _path = paths.File("trading-post-alerts.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                CheckOnce();
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // A bad tick shouldn't kill the loop; try again next interval.
                _logger.LogWarning(ex, "Trading-post alert check failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void CheckOnce()
    {
        if (_selected.AccountId is not { } accountId)
        {
            return;
        }

        HashSet<long> notified = Load();
        (List<TradingPostOrderRow> newlyBeaten, HashSet<long> beaten) =
            TradingPostAlerts.Check(_reader.GetTradingPostOrders(accountId), notified);

        if (newlyBeaten.Count > TradingPostAlerts.MaxIndividualNotifications)
        {
            (string title, string message) = TradingPostAlerts.Summarize(newlyBeaten);
            _notifier.Notify(title, message, NotificationCategories.TradingPost);
        }
        else
        {
            foreach (TradingPostOrderRow order in newlyBeaten)
            {
                (string title, string message) = TradingPostAlerts.Describe(order);
                _notifier.Notify(title, message, NotificationCategories.TradingPost);
            }
        }

        if (!beaten.SetEquals(notified))
        {
            Save(beaten);
        }
    }

    private HashSet<long> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<HashSet<long>>(File.ReadAllText(_path)) ?? new HashSet<long>()
                : new HashSet<long>();
        }
        catch (Exception)
        {
            return new HashSet<long>();
        }
    }

    private void Save(HashSet<long> beaten) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(beaten.Order().ToArray()));
}
