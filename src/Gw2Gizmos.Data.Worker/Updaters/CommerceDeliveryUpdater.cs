using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Commerce;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Polls the authenticated trading-post delivery box (<c>/v2/commerce/delivery</c>) and notifies
/// when new coins or items arrive (a sell/buy order filled, or a cancelled order returned items).
/// The box accumulates until the player collects it, so only <em>increases</em> are treated as a
/// new delivery; the box emptying on collection is ignored. The baseline (last-seen box) is
/// persisted via <see cref="IDeliveryBaselineStore"/>, so a delivery that arrived while the worker
/// was off is still detected on the next run.
/// </summary>
public sealed class CommerceDeliveryUpdater : BackgroundService
{
    // Most stacks listed in a single notification before the rest are summarised; a delivery box
    // can hold up to ~100 stacks, which is far too many to spell out.
    private const int MaxItemStacksShown = 5;
    private const int DefaultPollSeconds = 120;
    private const int MinPollSeconds = 15;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGw2ApiClientFactory _apiClientFactory;
    private readonly IGw2ApiKeyProvider _apiKeyProvider;
    private readonly IDeliveryBaselineStore _baselineStore;
    private readonly INotifier _notifier;
    private readonly ILogger<CommerceDeliveryUpdater> _logger;
    private readonly TimeSpan _pollInterval;

    private Gw2ApiClient? _apiClient;
    private string? _currentApiKey;
    private bool _warnedNoKey;
    private bool _baselineEstablished;
    private int _lastCoins;
    private Dictionary<int, int> _lastItemCounts = new();

    public CommerceDeliveryUpdater(
        IServiceScopeFactory scopeFactory,
        IGw2ApiClientFactory apiClientFactory,
        IGw2ApiKeyProvider apiKeyProvider,
        IDeliveryBaselineStore baselineStore,
        IConfiguration configuration,
        INotifier notifier,
        ILogger<CommerceDeliveryUpdater> logger
    )
    {
        _scopeFactory = scopeFactory;
        _apiClientFactory = apiClientFactory;
        _apiKeyProvider = apiKeyProvider;
        _baselineStore = baselineStore;
        _notifier = notifier;
        _logger = logger;

        int pollSeconds = Math.Max(MinPollSeconds, configuration.GetValue("Gw2:DeliveryPollSeconds", DefaultPollSeconds));
        _pollInterval = TimeSpan.FromSeconds(pollSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoadBaseline();

        using var timer = new PeriodicTimer(_pollInterval);
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

    /// <summary>
    /// Returns the API client for the current key, rebuilding it when the key changes and returning
    /// null when no key is available yet — so the poller simply idles until the user provides one
    /// (no restart needed).
    /// </summary>
    private Gw2ApiClient? ResolveApiClient()
    {
        string? apiKey = _apiKeyProvider.GetApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (!_warnedNoKey)
            {
                _logger.LogInformation(
                    "No GW2 API key available; trading-post delivery notifications are idle until one is provided."
                );
                _warnedNoKey = true;
            }

            _apiClient = null;
            _currentApiKey = null;
            return null;
        }

        if (!string.Equals(apiKey, _currentApiKey, StringComparison.Ordinal))
        {
            // Key needs the 'account' + 'tradingpost' scopes.
            _apiClient = _apiClientFactory.Create(apiKey, Locale.English);
            _currentApiKey = apiKey;
            _warnedNoKey = false;
            _logger.LogInformation("GW2 API key detected; trading-post delivery notifications are active.");
        }

        return _apiClient;
    }

    private async Task PollDeliveryAsync(CancellationToken stoppingToken)
    {
        Gw2ApiClient? apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return;
        }

        CommerceDelivery? delivery = await apiClient.V2.Commerce.Delivery.GetBlob(stoppingToken);
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
            _baselineStore.Save(new DeliveryBaseline(delivery.Coins, currentItems));
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

        bool boxChanged = delivery.Coins != _lastCoins || !ItemsEqual(currentItems, _lastItemCounts);

        _lastCoins = delivery.Coins;
        _lastItemCounts = currentItems;

        // Persist on any change (a collection empties the box too) so the baseline survives restart.
        if (boxChanged)
        {
            _baselineStore.Save(new DeliveryBaseline(delivery.Coins, currentItems));
        }

        if (coinsDelta <= 0 && addedItems.Count == 0)
        {
            return;
        }

        string message = await BuildMessageAsync(coinsDelta, addedItems, stoppingToken);
        _notifier.Notify("Trading post delivery", message, "Delivery");
    }

    private void LoadBaseline()
    {
        DeliveryBaseline? baseline = _baselineStore.Load();
        if (baseline is null)
        {
            return;
        }

        _lastCoins = baseline.Coins;
        _lastItemCounts = baseline.Items ?? new Dictionary<int, int>();
        _baselineEstablished = true;
        _logger.LogInformation(
            "Loaded persisted delivery baseline: {Coins} coins, {StackCount} stack(s).",
            _lastCoins,
            _lastItemCounts.Count
        );
    }

    private static bool ItemsEqual(Dictionary<int, int> a, Dictionary<int, int> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach ((int id, int count) in a)
        {
            if (b.GetValueOrDefault(id) != count)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string> BuildMessageAsync(
        int coinsDelta,
        List<(int Id, int Count)> addedItems,
        CancellationToken stoppingToken
    )
    {
        var parts = new List<string>();

        // Coins are always shown in full — never truncated.
        if (coinsDelta > 0)
        {
            parts.Add($"+{FormatCoins(coinsDelta)}");
        }

        if (addedItems.Count > 0)
        {
            // Largest stacks first; only the shown ones need a name lookup.
            List<(int Id, int Count)> shown = addedItems.OrderByDescending(a => a.Count).Take(MaxItemStacksShown).ToList();
            int[] ids = shown.Select(s => s.Id).ToArray();

            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            Dictionary<int, string> names = await dbContext
                .Items.Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name, stoppingToken);

            foreach ((int id, int count) in shown)
            {
                string name = names.GetValueOrDefault(id) ?? $"item #{id}";
                parts.Add($"{count}x {name}");
            }

            int remainingStacks = addedItems.Count - shown.Count;
            if (remainingStacks > 0)
            {
                parts.Add($"+{remainingStacks} more stack{(remainingStacks == 1 ? "" : "s")}");
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
