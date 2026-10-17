using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApiCommerce = Gw2Gizmos.Gw2Api.Contract.V2.Commerce;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Checks the price alerts against the trading post and notifies when one triggers (<see cref="PriceAlerts"/>). It
/// polls <c>/v2/commerce/prices</c> (public, no key) for just the alerted items every few minutes — independent of
/// the worker's price history, which is off by default — and right away when an alert is added or edited. Runs in
/// the desktop process because that owns the OS notifications.
/// </summary>
public sealed class PriceAlertService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    // The prices endpoint takes up to 200 ids per request.
    private const int IdsPerRequest = 200;

    private readonly PriceAlertStore _store;
    private readonly IGw2ApiClientFactory _clientFactory;
    private readonly INotifier _notifier;
    private readonly ILogger<PriceAlertService> _logger;
    private readonly SemaphoreSlim _wake = new(0);

    public PriceAlertService(
        PriceAlertStore store, IGw2ApiClientFactory clientFactory, INotifier notifier, ILogger<PriceAlertService> logger)
    {
        _store = store;
        _clientFactory = clientFactory;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A new or edited alert hasn't been checked yet: check now rather than at the next poll.
        _store.Changed += () =>
        {
            if (_store.All.Any(a => a.LastCheckedUtc is null) && _wake.CurrentCount == 0)
            {
                _wake.Release();
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Price alert check failed; trying again at the next poll.");
            }

            try
            {
                await Task.WhenAny(Task.Delay(PollInterval, stoppingToken), _wake.WaitAsync(stoppingToken));
            }
            catch (OperationCanceledException) { }
        }
    }

    private async Task CheckAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<PriceAlert> alerts = _store.All;
        if (alerts.Count == 0)
        {
            return;
        }

        Gw2ApiClient client = _clientFactory.Create(Locale.English);
        var prices = new Dictionary<int, ApiCommerce.CommercePrices>();
        foreach (int[] ids in alerts.Select(a => a.ItemId).Distinct().Chunk(IdsPerRequest))
        {
            foreach (ApiCommerce.CommercePrices price in (await client.V2.Commerce.Prices.GetByIds(ids, stoppingToken)).Content ?? [])
            {
                prices[price.Id] = price;
            }
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var updated = new List<PriceAlert>();
        var triggered = new List<PriceAlert>();
        foreach (PriceAlert alert in alerts)
        {
            (PriceAlert checkedAlert, bool fired) = PriceAlerts.Apply(alert, PriceOf(prices.GetValueOrDefault(alert.ItemId), alert.Side), now);
            updated.Add(checkedAlert);
            if (fired)
            {
                triggered.Add(checkedAlert);
            }
        }

        _store.Update(updated);
        foreach (PriceAlert alert in triggered)
        {
            _notifier.Notify(
                $"Price alert: {alert.ItemName}",
                $"{(alert.Side == PriceAlertSide.Sell ? "Sell" : "Buy")} price is {Coin.Format(alert.LastPrice ?? 0)}, "
                + $"{(alert.Direction == PriceAlertDirection.Below ? "at or below" : "at or above")} your {Coin.Format(alert.PriceCopper)}.",
                NotificationCategories.PriceAlerts);
        }
    }

    /// <summary>The watched side's best price, or null when nobody's buying / selling (or the item isn't listed).</summary>
    private static int? PriceOf(ApiCommerce.CommercePrices? prices, PriceAlertSide side)
    {
        ApiCommerce.CommercePrice? book = side == PriceAlertSide.Buy ? prices?.Buys : prices?.Sells;
        return book is { Quantity: > 0 } ? book.UnitPrice : null;
    }
}
