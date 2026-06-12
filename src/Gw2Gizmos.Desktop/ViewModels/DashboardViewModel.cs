using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.Extensions.DependencyInjection;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Desktop;

public sealed class DashboardViewModel : ViewModelBase
{
    /// <summary>Scopes this app needs; highlighted on the dashboard and flagged when missing.</summary>
    private static readonly string[] RequiredScopes = { "account", "tradingpost" };

    private readonly AppStateApiKeyStore _apiKeyStore;
    private string _apiKeyStatus;

    public DashboardViewModel(IServiceScopeFactory scopeFactory, AppStateApiKeyStore apiKeyStore)
    {
        _apiKeyStore = apiKeyStore;
        ApiKeyConfigured = apiKeyStore.HasApiKey;
        _apiKeyStatus = ApiKeyConfigured ? "Checking…" : "Not set";

        using IServiceScope scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        ItemCount = dbContext.Items.Count();
        RecipeCount = dbContext.Recipes.Count();
        TradeableItemCount = dbContext.CommerceItemListings.Count();
        CurrencyCount = dbContext.Currencies.Count();
        PriceSnapshotCount = dbContext.PriceSnapshots.Count();
        NotificationCount = dbContext.Notifications.Count();

        // The price poller runs every 5 min, so a recent snapshot means the worker is alive and updating.
        DateTimeOffset? lastPoll = dbContext
            .PriceSnapshots.OrderByDescending(p => p.Id)
            .Select(p => (DateTimeOffset?)p.TimestampUtc)
            .FirstOrDefault();
        WorkerOperational = lastPoll is { } poll && DateTimeOffset.UtcNow - poll < TimeSpan.FromMinutes(15);

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

    public int NotificationCount { get; }

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

        var granted = info.Permissions.Select(p => p.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Scopes.Clear();
        foreach (string scope in granted.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            Scopes.Add(new ScopeBadge(scope, RequiredScopes.Contains(scope, StringComparer.OrdinalIgnoreCase), true));
        }

        foreach (string required in RequiredScopes.Where(r => !granted.Contains(r)))
        {
            Scopes.Add(new ScopeBadge(required, true, false));
        }
    }
}

/// <summary>One API-key scope for the dashboard: granted-and-required (good), granted-extra, or missing.</summary>
public sealed record ScopeBadge(string Name, bool IsRequired, bool IsGranted);
