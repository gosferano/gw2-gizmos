using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
    private int _itemCount;
    private int _recipeCount;
    private int _tradeableItemCount;
    private int _currencyCount;
    private int _priceSnapshotCount;
    private bool _workerOperational;
    private string _accountName = "—";
    private string _accountSynced = "";
    private int _characterCount;
    private long _coinBalance;
    private int _sessionCount;
    private string _playtime = "—";
    private string _sessionStatus = "No sessions yet";
    private bool _hasSessions;
    private string _workerUpdated = "";

    public DashboardViewModel(
        IServiceScopeFactory scopeFactory,
        FileGw2ApiKeyStore apiKeyStore,
        FeatureSettingsStore features,
        AccountReader reader,
        AppPaths paths,
        UpdateStatus updateStatus
    )
    {
        _apiKeyStore = apiKeyStore;
        _features = features;
        ApiKeyConfigured = apiKeyStore.HasApiKey;
        _apiKeyStatus = ApiKeyConfigured ? "Checking…" : "Not set";

        UpdateReady = updateStatus.UpdateReady;
        UpdateText = updateStatus.UpdateReady ? $"Update {updateStatus.PendingVersion} ready" : "";
        RestartCommand = new RelayCommand(updateStatus.ApplyAndRestart);

        System.Reflection.Assembly assembly = typeof(DashboardViewModel).Assembly;
        string informational = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "—";
        // Trim the +<git-sha> build-metadata suffix for a clean display.
        AppVersion = informational.Split('+')[0];
#if DEBUG
        AppEnvironment = "Development build";
#else
        AppEnvironment = "Release";
#endif
        DatabaseSize = MeasureDatabaseSize(paths);

        // The stat queries scan large tables (item catalog, full price history) against a DB that's cold on first
        // open — so they run off the UI thread and populate the cards when ready, rather than freezing navigation.
        _ = LoadCountsAsync(scopeFactory);
        _ = LoadAccountStatsAsync(reader);
        _ = LoadTokenInfoAsync();
    }

    private async Task LoadCountsAsync(IServiceScopeFactory scopeFactory)
    {
        DashboardCounts counts = await Task.Run(() => ReadCounts(scopeFactory));

        // Back on the UI thread (post-await) — assigning raises PropertyChanged so the cards update.
        ItemCount = counts.Items;
        RecipeCount = counts.Recipes;
        TradeableItemCount = counts.Tradeable;
        CurrencyCount = counts.Currencies;
        PriceSnapshotCount = counts.PriceSnapshots;
        WorkerOperational = counts.WorkerOperational;
        WorkerUpdated = FormatUpdated(counts.LastPollUtc);
    }

    /// <summary>The on-disk size of the SQLite database (+ its WAL), or "—" if it can't be read.</summary>
    private static string MeasureDatabaseSize(AppPaths paths)
    {
        try
        {
            long bytes = 0;
            foreach (string suffix in new[] { "", "-wal" })
            {
                var info = new System.IO.FileInfo(paths.File("gw2gizmos.sqlite") + suffix);
                if (info.Exists)
                {
                    bytes += info.Length;
                }
            }

            return bytes == 0 ? "—" : FormatBytes(bytes);
        }
        catch (Exception)
        {
            return "—";
        }
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.0} GB"
        : bytes >= 1L << 20 ? $"{bytes / (double)(1L << 20):0.0} MB"
        : $"{Math.Max(1, bytes / 1024)} KB";

    private static string FormatUpdated(DateTimeOffset? lastPollUtc)
    {
        if (lastPollUtc is not { } poll)
        {
            return "No data yet";
        }

        TimeSpan ago = DateTimeOffset.UtcNow - poll;
        return ago < TimeSpan.FromMinutes(1) ? "Updated just now"
            : ago < TimeSpan.FromHours(1) ? $"Updated {(int)ago.TotalMinutes}m ago"
            : ago < TimeSpan.FromDays(1) ? $"Updated {(int)ago.TotalHours}h ago"
            : $"Updated {(int)ago.TotalDays}d ago";
    }

    private async Task LoadAccountStatsAsync(AccountReader reader)
    {
        (AccountInfo? account, int characters, long coin, DashboardSessionStats sessions) = await Task.Run(() =>
        {
            AccountInfo? current = reader.GetCurrentAccount();
            return current is null
                ? (null, 0, 0L, new DashboardSessionStats(0, TimeSpan.Zero, null, false, null))
                : (current, reader.GetCharacterCount(current.Id), reader.GetCoinBalance(current.Id),
                    reader.GetSessionStats(current.Id));
        });

        if (account is not null)
        {
            AccountName = account.Name;
            AccountSynced = $"Synced {account.LastSyncedUtc.LocalDateTime:g}";
            CharacterCount = characters;
            CoinBalance = coin;
        }

        SessionCount = sessions.Count;
        HasSessions = sessions.Count > 0;
        Playtime = FormatPlaytime(sessions.TotalPlaytime);
        SessionStatus = sessions.Count == 0
            ? "No sessions yet"
            : sessions.IsPlaying
                ? $"Playing now · {sessions.Character ?? "?"}"
                : $"Last played {sessions.LastPlayedUtc?.LocalDateTime:g} · {sessions.Character ?? "?"}";
    }

    private static string FormatPlaytime(TimeSpan span)
    {
        if (span < TimeSpan.FromMinutes(1))
        {
            return "—";
        }

        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : $"{span.Minutes}m";
    }

    /// <summary>
    /// Reads the dashboard counts. The worker owns the database and we open it read-only; on a fresh install it
    /// may not exist yet (the worker creates it shortly after launch), so an absent/locked DB yields zeros rather
    /// than throwing — the dashboard then shows zeros until the first sync lands.
    /// </summary>
    private static DashboardCounts ReadCounts(IServiceScopeFactory scopeFactory)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            int items = dbContext.Items.Count();
            int recipes = dbContext.Recipes.Count();
            // Distinct items that have ever been price-polled (on the trading post); derived from snapshots
            // since the order-book table is gone.
            int tradeable = dbContext.PriceSnapshots.AsNoTracking().Select(s => s.ItemId).Distinct().Count();
            int currencies = dbContext.Currencies.Count();
            int priceSnapshots = dbContext.PriceSnapshots.Count();

            // The price poller runs every 5 min, so a recent snapshot means the worker is alive and updating.
            DateTimeOffset? lastPoll = dbContext
                .PriceSnapshots.OrderByDescending(p => p.Id)
                .Select(p => (DateTimeOffset?)p.TimestampUtc)
                .FirstOrDefault();
            bool operational = lastPoll is { } poll && DateTimeOffset.UtcNow - poll < TimeSpan.FromMinutes(15);

            return new DashboardCounts(items, recipes, tradeable, currencies, priceSnapshots, operational, lastPoll);
        }
        catch (Exception)
        {
            // Database not ready yet (fresh install) — counts stay zero, worker shows not-operational.
            return default;
        }
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
    public bool WorkerOperational
    {
        get => _workerOperational;
        private set => SetProperty(ref _workerOperational, value);
    }

    /// <summary>The token's granted permissions (plus any required-but-missing), shown as badges.</summary>
    public ObservableCollection<ScopeBadge> Scopes { get; } = new();

    public int ItemCount
    {
        get => _itemCount;
        private set => SetProperty(ref _itemCount, value);
    }

    public int RecipeCount
    {
        get => _recipeCount;
        private set => SetProperty(ref _recipeCount, value);
    }

    public int TradeableItemCount
    {
        get => _tradeableItemCount;
        private set => SetProperty(ref _tradeableItemCount, value);
    }

    public int CurrencyCount
    {
        get => _currencyCount;
        private set => SetProperty(ref _currencyCount, value);
    }

    public int PriceSnapshotCount
    {
        get => _priceSnapshotCount;
        private set => SetProperty(ref _priceSnapshotCount, value);
    }

    /// <summary>Relative time of the worker's last price poll, e.g. "Updated 3m ago".</summary>
    public string WorkerUpdated
    {
        get => _workerUpdated;
        private set => SetProperty(ref _workerUpdated, value);
    }

    // --- App ---
    public string AppVersion { get; }

    public string AppEnvironment { get; }

    /// <summary>On-disk size of the SQLite database (+ WAL).</summary>
    public string DatabaseSize { get; }

    /// <summary>True when an update has been downloaded and will apply on the next restart.</summary>
    public bool UpdateReady { get; }

    /// <summary>"Update X.Y.Z ready", or empty.</summary>
    public string UpdateText { get; }

    /// <summary>Applies the staged update and restarts the app.</summary>
    public RelayCommand RestartCommand { get; }

    // --- Account ---
    public string AccountName
    {
        get => _accountName;
        private set => SetProperty(ref _accountName, value);
    }

    public string AccountSynced
    {
        get => _accountSynced;
        private set => SetProperty(ref _accountSynced, value);
    }

    public int CharacterCount
    {
        get => _characterCount;
        private set => SetProperty(ref _characterCount, value);
    }

    /// <summary>The account's coin balance in copper (formatted as g/s/c in the view).</summary>
    public long CoinBalance
    {
        get => _coinBalance;
        private set => SetProperty(ref _coinBalance, value);
    }

    // --- Play sessions ---
    public bool HasSessions
    {
        get => _hasSessions;
        private set => SetProperty(ref _hasSessions, value);
    }

    public int SessionCount
    {
        get => _sessionCount;
        private set => SetProperty(ref _sessionCount, value);
    }

    public string Playtime
    {
        get => _playtime;
        private set => SetProperty(ref _playtime, value);
    }

    /// <summary>"Playing now · X", "Last played … · X", or "No sessions yet".</summary>
    public string SessionStatus
    {
        get => _sessionStatus;
        private set => SetProperty(ref _sessionStatus, value);
    }

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

/// <summary>The dashboard's DB-derived counts, read off the UI thread.</summary>
internal readonly record struct DashboardCounts(
    int Items,
    int Recipes,
    int Tradeable,
    int Currencies,
    int PriceSnapshots,
    bool WorkerOperational,
    DateTimeOffset? LastPollUtc
);
