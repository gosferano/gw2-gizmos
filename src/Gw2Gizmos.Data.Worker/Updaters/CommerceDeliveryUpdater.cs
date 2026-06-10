using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Polls the authenticated trading-post delivery box (<c>/v2/commerce/delivery</c>) and notifies
/// when new coins or items arrive (a sell/buy order filled, or a cancelled order returned items).
/// The box accumulates until the player collects it, so only <em>increases</em> are treated as a
/// new delivery; the box emptying on collection is ignored. The baseline is kept in memory: the
/// first poll establishes it without notifying.
/// </summary>
public sealed class CommerceDeliveryUpdater : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotifier _notifier;
    private readonly ILogger<CommerceDeliveryUpdater> _logger;
    private readonly Gw2ApiClient? _apiClient;

    private bool _baselineEstablished;
    private int _lastCoins;
    private Dictionary<int, int> _lastItemCounts = new();

    public CommerceDeliveryUpdater(
        IServiceScopeFactory scopeFactory,
        IGw2ApiClientFactory apiClientFactory,
        IConfiguration configuration,
        INotifier notifier,
        ILogger<CommerceDeliveryUpdater> logger
    )
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;

        // Key needs the 'account' + 'tradingpost' scopes. Set it in user-secrets ("Gw2:ApiKey")
        // or via the GW2_API_KEY environment variable.
        string? apiKey = configuration["Gw2:ApiKey"] ?? configuration["GW2_API_KEY"];
        _apiClient = string.IsNullOrWhiteSpace(apiKey) ? null : apiClientFactory.Create(apiKey, Locale.English);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_apiClient is null)
        {
            _logger.LogWarning("No GW2 API key configured; trading-post delivery notifications are disabled.");
            return;
        }

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await PollDeliveryAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling trading-post delivery.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollDeliveryAsync(CancellationToken stoppingToken)
    {
        CommerceDelivery? delivery = await _apiClient!.V2.Commerce.Delivery.GetBlob(stoppingToken);
        if (delivery is null)
        {
            _logger.LogWarning(
                "Trading-post delivery request returned no data; check the API key has the 'tradingpost' scope."
            );
            return;
        }

        Dictionary<int, int> currentItems = delivery.Items.ToDictionary(i => i.Id, i => i.Count);

        if (!_baselineEstablished)
        {
            _lastCoins = delivery.Coins;
            _lastItemCounts = currentItems;
            _baselineEstablished = true;
            _logger.LogInformation(
                "Trading-post delivery baseline established: {Coins} coins, {StackCount} stack(s).",
                delivery.Coins,
                currentItems.Count
            );
            return;
        }

        // Only increases are a new delivery; a decrease means the player collected the box.
        int coinsDelta = delivery.Coins - _lastCoins;
        var addedItems = new List<(int Id, int Count)>();
        foreach ((int id, int count) in currentItems)
        {
            int delta = count - _lastItemCounts.GetValueOrDefault(id);
            if (delta > 0)
            {
                addedItems.Add((id, delta));
            }
        }

        _lastCoins = delivery.Coins;
        _lastItemCounts = currentItems;

        if (coinsDelta <= 0 && addedItems.Count == 0)
        {
            return;
        }

        string message = await BuildMessageAsync(coinsDelta, addedItems, stoppingToken);
        _notifier.Notify("Trading post delivery", message);
    }

    private async Task<string> BuildMessageAsync(
        int coinsDelta,
        List<(int Id, int Count)> addedItems,
        CancellationToken stoppingToken
    )
    {
        var parts = new List<string>();
        if (coinsDelta > 0)
        {
            parts.Add($"+{FormatCoins(coinsDelta)}");
        }

        if (addedItems.Count > 0)
        {
            int[] ids = addedItems.Select(a => a.Id).ToArray();

            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            Dictionary<int, string> names = await dbContext
                .Items.Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name, stoppingToken);

            foreach ((int id, int count) in addedItems)
            {
                string name = names.GetValueOrDefault(id) ?? $"item #{id}";
                parts.Add($"{count}x {name}");
            }
        }

        return string.Join(", ", parts);
    }

    private static string FormatCoins(int coins)
    {
        int gold = coins / 10000;
        int silver = coins / 100 % 100;
        int copper = coins % 100;

        var parts = new List<string>();
        if (gold > 0)
        {
            parts.Add($"{gold}g");
        }
        if (silver > 0)
        {
            parts.Add($"{silver}s");
        }
        if (copper > 0 || parts.Count == 0)
        {
            parts.Add($"{copper}c");
        }

        return string.Join(" ", parts);
    }
}
