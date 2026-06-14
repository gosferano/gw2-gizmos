using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Desktop;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly FileGw2ApiKeyStore _apiKeyStore;
    private readonly FeatureSettingsStore _features;
    private string _apiKeyStatus;

    public DashboardViewModel(
        IServiceScopeFactory scopeFactory,
        FileGw2ApiKeyStore apiKeyStore,
        FeatureSettingsStore features
    )
    {
        _apiKeyStore = apiKeyStore;
        _features = features;
        ApiKeyConfigured = apiKeyStore.HasApiKey;
        _apiKeyStatus = ApiKeyConfigured ? "Checking…" : "Not set";

        // The worker owns the database and opens it read-only here; on a fresh install it may not exist yet
        // (the worker creates it shortly after launch). Treat an absent/locked DB as "no data yet" rather
        // than crashing — the dashboard then shows zeros until the first sync lands.
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            ItemCount = dbContext.Items.Count();
            RecipeCount = dbContext.Recipes.Count();
            // Distinct items that have ever been price-polled (on the trading post); derived from snapshots
            // since the order-book table is gone.
            TradeableItemCount = dbContext.PriceSnapshots.AsNoTracking().Select(s => s.ItemId).Distinct().Count();
            CurrencyCount = dbContext.Currencies.Count();
            PriceSnapshotCount = dbContext.PriceSnapshots.Count();

            // The price poller runs every 5 min, so a recent snapshot means the worker is alive and updating.
            DateTimeOffset? lastPoll = dbContext
                .PriceSnapshots.OrderByDescending(p => p.Id)
                .Select(p => (DateTimeOffset?)p.TimestampUtc)
                .FirstOrDefault();
            WorkerOperational = lastPoll is { } poll && DateTimeOffset.UtcNow - poll < TimeSpan.FromMinutes(15);
        }
        catch (Exception)
        {
            // Database not ready yet (fresh install) — counts stay zero, worker shows not-operational.
        }

        _ = LoadTokenInfoAsync();
    }

    /// <summary>Configured/not-set state — becomes the token name once verified against the API.</summary>
    public string ApiKeyStatus
    {
        get => _apiKeyStatus;
        private set => SetProperty(ref _apiKeyStatus, value);
    }

    /// <summary>True when an API key is stored — drives the API-key card's status dot.</summary>
    public bool ApiKeyConfigured { get; }

    /// <summary>True when the worker has produced data recently — drives the Worker card's status dot.</summary>
    public bool WorkerOperational { get; }

    /// <summary>The token's granted scopes (plus any required-but-missing), shown as badges.</summary>
    public ObservableCollection<ScopeBadge> Scopes { get; } = new();

    public int ItemCount { get; }

    public int RecipeCount { get; }

    public int TradeableItemCount { get; }

    public int CurrencyCount { get; }

    public int PriceSnapshotCount { get; }

    private async Task LoadTokenInfoAsync()
    {
        string? key = _apiKeyStore.GetApiKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            ApiKeyStatus = "Not set";
            return;
        }

        ApiTokenInfo? info = null;
        try
        {
            info = await Gw2ApiClient.Create(key, Locale.English).V2.TokenInfo.GetBlob();
        }
        catch
        {
            // Network/auth failure handled below.
        }

        if (info is null)
        {
            ApiKeyStatus = "Could not verify";
            return;
        }

        ApiKeyStatus = string.IsNullOrWhiteSpace(info.Name) ? "Configured" : info.Name;

        // "Required" is whatever the currently-enabled features need — so a permission like tradingpost is only
        // flagged as required (or missing) while its feature (TP delivery alerts) is on.
        List<string> enabledFeatures = WorkerFeatures.All
            .Where(feature => _features.IsEnabled(feature.Key))
            .Select(feature => feature.Key)
            .ToList();
        var required = new HashSet<string>(
            WorkerFeatures.RequiredPermissions(enabledFeatures),
            StringComparer.OrdinalIgnoreCase
        );

        var granted = info.Permissions.Select(p => p.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Scopes.Clear();
        foreach (string scope in granted.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            Scopes.Add(new ScopeBadge(scope, required.Contains(scope), true));
        }

        foreach (string missing in required.Where(r => !granted.Contains(r)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            Scopes.Add(new ScopeBadge(missing, true, false));
        }
    }
}

/// <summary>One API-key scope for the dashboard: granted-and-required (good), granted-extra, or missing.</summary>
public sealed record ScopeBadge(string Name, bool IsRequired, bool IsGranted);
