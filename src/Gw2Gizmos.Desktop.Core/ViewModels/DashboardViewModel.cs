using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Desktop.Core;

public sealed class DashboardViewModel : ViewModelBase, INavigationAware
{
    // The catalog counts scan large tables (price history), so live refreshes of them are spaced out.
    private static readonly TimeSpan CountsRefreshInterval = TimeSpan.FromSeconds(30);

    // How often the live session card looks at the game (a read of the MumbleLink block — cheap).
    private static readonly TimeSpan LivePollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AccountReader _reader;
    private readonly WorkerProcessLauncher _worker;
    private readonly IDisposable _dataChanges;
    private readonly LiveGameMonitor _liveGame;
    private readonly MapNames _mapNames;
    private readonly IUiTimer _livePoll;
    private string? _accountId;
    private long? _openSessionId;
    private SessionValueSummary? _openSessionValue;
    private int _liveMapId;
    private bool _mapLookupPending;
    private bool _isLiveVisible;
    private bool _isLiveActive;
    private string _liveCharacter = "";
    private string _liveMap = "";
    private string _liveDuration = "";
    private DateTimeOffset _countsLoadedAt;
    private bool _loadingCounts;
    private bool _loadingAccountStats;
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
    private string? _accountSyncedTooltip;
    private int _characterCount;
    private long _coinBalance;
    private int _sessionCount;
    private string _playtime = "—";
    private string _sessionStatus = "No sessions yet";
    private string? _sessionStatusTooltip;
    private bool _hasSessions;
    private string _workerUpdated = "";
    private string? _workerUpdatedTooltip;
    private bool _updateReady;
    private string _updateText = "";

    public DashboardViewModel(
        IServiceScopeFactory scopeFactory,
        FileGw2ApiKeyStore apiKeyStore,
        FeatureSettingsStore features,
        AccountReader reader,
        AppPaths paths,
        UpdateStatus updateStatus,
        IUiDispatcher dispatcher,
        DataChangeMonitor dataChanges,
        WorkerProcessLauncher worker,
        LiveGameMonitor liveGame,
        MapNames mapNames
    )
    {
        _liveGame = liveGame;
        _mapNames = mapNames;
        _scopeFactory = scopeFactory;
        _worker = worker;
        _reader = reader;
        _apiKeyStore = apiKeyStore;
        _features = features;
        ApiKeyConfigured = apiKeyStore.HasApiKey;
        _apiKeyStatus = ApiKeyConfigured ? "Checking…" : "Not set";

        RestartCommand = new RelayCommand(updateStatus.ApplyAndRestart);
        ApplyUpdateStatus(updateStatus);
        // The startup update check runs in the background and completes after this VM is built and shown, so
        // reflect its result reactively rather than only snapshotting it here (off the UI thread → marshal).
        updateStatus.Changed += () => dispatcher.Invoke(() => ApplyUpdateStatus(updateStatus));

        // The app's version is stamped on the UI head's exe (the entry assembly), not on this shared library.
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(DashboardViewModel).Assembly;
        string informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
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
        WorkerOperational = worker.IsRunning;
        _ = LoadCountsAsync();
        _ = LoadAccountStatsAsync();
        _ = LoadTokenInfoAsync();

        // Refresh in place as the worker syncs, sessions start/end, and relative times ("synced 2 min ago") age.
        _dataChanges = dataChanges.Subscribe(OnDataChanged);

        // The live session card follows the game directly, so it shows up the moment a character enters the world.
        _livePoll = dispatcher.CreateTimer(LivePollInterval, PollLiveGame);
        _livePoll.Start();
    }

    public void OnNavigatedFrom()
    {
        _dataChanges.Dispose();
        _livePoll.Stop();
    }

    // --- Live session card ---

    /// <summary>A character is in the world right now: the card shows.</summary>
    public bool IsLiveVisible
    {
        get => _isLiveVisible;
        private set => SetProperty(ref _isLiveVisible, value);
    }

    /// <summary>The worker has opened the session: duration and loot so far are shown.</summary>
    public bool IsLiveActive
    {
        get => _isLiveActive;
        private set
        {
            if (SetProperty(ref _isLiveActive, value))
            {
                OnPropertyChanged(nameof(IsLiveStarting));
            }
        }
    }

    /// <summary>In the world but no session recorded yet: the worker's start-of-session account sync is running.</summary>
    public bool IsLiveStarting => !_isLiveActive;

    /// <summary>What "starting" means right now: the sync in progress, or that Play sessions is off.</summary>
    public string LiveStartingText => _features.IsEnabled(WorkerFeatures.PlaySessions.Key)
        ? "Starting session… syncing your account so loot counts from now"
        : "Play sessions is off. Turn it on in Settings to record this session.";

    public string LiveCharacter
    {
        get => _liveCharacter;
        private set => SetProperty(ref _liveCharacter, value);
    }

    /// <summary>The current map's name; empty until it's looked up.</summary>
    public string LiveMap
    {
        get => _liveMap;
        private set => SetProperty(ref _liveMap, value);
    }

    /// <summary>How long the session has run, e.g. "1h 05m".</summary>
    public string LiveDuration
    {
        get => _liveDuration;
        private set => SetProperty(ref _liveDuration, value);
    }

    /// <summary>Coin gained this session, in copper (drawn with coin icons).</summary>
    public long? LiveCoin => _openSessionValue?.CoinDeltaCopper;

    /// <summary>The instant-sell value of item loot this session, in copper.</summary>
    public long? LiveLoot => _openSessionValue?.LootValueCopper;

    /// <summary>Total value per hour so far, in copper; null in the first minute.</summary>
    public long? LiveGoldPerHour => _openSessionValue?.GoldPerHourCopper;

    public bool HasLiveGoldPerHour => LiveGoldPerHour is not null;

    private void PollLiveGame()
    {
        LiveGameState game = _liveGame.Read();
        if (!game.IsRunning || game.Character is null)
        {
            // Not running, at character select, or loading: nothing live to show.
            IsLiveVisible = false;
            return;
        }

        LiveCharacter = game.Character;
        if (game.MapId != _liveMapId)
        {
            _liveMapId = game.MapId;
            LiveMap = "";
        }

        // Look the name up on a map change, and keep retrying while it's missing (the worker's first maps sync may
        // not have landed yet).
        if (LiveMap.Length == 0 && !_mapLookupPending)
        {
            _ = LoadMapNameAsync(game.MapId);
        }

        IsLiveActive = _openSessionValue is not null;
        if (_openSessionValue is { } value)
        {
            LiveDuration = FormatSessionDuration(value.PlayedTime(DateTimeOffset.UtcNow));
            OnPropertyChanged(nameof(LiveGoldPerHour));
            OnPropertyChanged(nameof(HasLiveGoldPerHour));
        }
        else
        {
            OnPropertyChanged(nameof(LiveStartingText));
        }

        IsLiveVisible = true;
    }

    private async Task LoadMapNameAsync(int mapId)
    {
        _mapLookupPending = true;
        try
        {
            string? name = await _mapNames.GetAsync(mapId);
            if (mapId == _liveMapId)
            {
                LiveMap = name ?? "";
            }
        }
        finally
        {
            _mapLookupPending = false;
        }
    }

    /// <summary>Loads the open session's worth (coin, loot, gold/hour) for the live card; clears it once closed.</summary>
    private async Task LoadOpenSessionValueAsync(long? sessionId)
    {
        _openSessionId = sessionId;
        SessionValueSummary? value = null;
        if (sessionId is { } id && _accountId is { } accountId)
        {
            value = await Task.Run(() => _reader.GetSessionValue(accountId, id));
        }

        if (sessionId != _openSessionId)
        {
            return; // a newer load has started
        }

        _openSessionValue = value;
        OnPropertyChanged(nameof(LiveCoin));
        OnPropertyChanged(nameof(LiveLoot));
        OnPropertyChanged(nameof(LiveGoldPerHour));
        OnPropertyChanged(nameof(HasLiveGoldPerHour));
    }

    private static string FormatSessionDuration(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{Math.Max(0, span.Minutes)}m";

    private void OnDataChanged(DataChanges changes)
    {
        // Cheap, so checked on every notification (at least once a minute): goes grey soon after the worker dies.
        WorkerOperational = _worker.IsRunning;

        if ((changes & (DataChanges.AccountSync | DataChanges.Holdings | DataChanges.Sessions | DataChanges.Clock
                | DataChanges.AccountValue)) != 0)
        {
            _ = LoadAccountStatsAsync();
        }

        if ((changes & (DataChanges.Catalog | DataChanges.Clock)) != 0
            && DateTimeOffset.UtcNow - _countsLoadedAt >= CountsRefreshInterval)
        {
            _ = LoadCountsAsync();
        }
    }

    private void ApplyUpdateStatus(UpdateStatus updateStatus)
    {
        UpdateReady = updateStatus.UpdateReady;
        UpdateText = updateStatus.UpdateReady ? $"Update {updateStatus.PendingVersion} ready" : "";
    }

    private async Task LoadCountsAsync()
    {
        if (_loadingCounts)
        {
            return;
        }

        _loadingCounts = true;
        DashboardCounts counts;
        try
        {
            counts = await Task.Run(() => ReadCounts(_scopeFactory));
        }
        finally
        {
            _loadingCounts = false;
            _countsLoadedAt = DateTimeOffset.UtcNow;
        }

        // Back on the UI thread (post-await) — assigning raises PropertyChanged so the cards update.
        ItemCount = counts.Items;
        RecipeCount = counts.Recipes;
        TradeableItemCount = counts.Tradeable;
        CurrencyCount = counts.Currencies;
        PriceSnapshotCount = counts.PriceSnapshots;
        WorkerOperational = _worker.IsRunning;
        WorkerUpdated = FormatUpdated(counts.LastWriteUtc);
        WorkerUpdatedTooltip = counts.LastWriteUtc is { } write ? RelativeTime.Exact(write) : null;
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

    private static string FormatUpdated(DateTimeOffset? lastWriteUtc) =>
        lastWriteUtc is { } write ? $"Updated {RelativeTime.Format(write)}" : "No data yet";

    private async Task LoadAccountStatsAsync()
    {
        if (_loadingAccountStats)
        {
            return;
        }

        _loadingAccountStats = true;
        (AccountInfo? account, int characters, long coin, DashboardSessionStats sessions, AccountValueSummary? value) result;
        try
        {
            result = await Task.Run(() =>
            {
                AccountInfo? current = _reader.GetCurrentAccount();
                return current is null
                    ? (null, 0, 0L, new DashboardSessionStats(0, TimeSpan.Zero, null, false, null), null)
                    : (current, _reader.GetCharacterCount(current.Id), _reader.GetCoinBalance(current.Id),
                        _reader.GetSessionStats(current.Id), _reader.GetAccountValueSummary(current.Id));
            });
        }
        finally
        {
            _loadingAccountStats = false;
        }

        (AccountInfo? account, int characters, long coin, DashboardSessionStats sessions, AccountValueSummary? value) = result;
        AccountValue = value;
        if (account is not null)
        {
            AccountName = account.Name;
            AccountSynced = $"Synced {RelativeTime.Format(account.LastSyncedUtc)}";
            AccountSyncedTooltip = RelativeTime.Exact(account.LastSyncedUtc);
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
                : $"Last played {(sessions.LastPlayedUtc is { } lastPlayed ? RelativeTime.Format(lastPlayed) : "—")} · {sessions.Character ?? "?"}";
        SessionStatusTooltip = !sessions.IsPlaying && sessions.LastPlayedUtc is { } played ? RelativeTime.Exact(played) : null;

        _accountId = account?.Id;
        await LoadOpenSessionValueAsync(sessions.OpenSessionId);
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

            // "Updated …": the worker's latest periodic write — a price poll (only with Price history on) or an
            // account sync (only with an API key), whichever is newer.
            DateTimeOffset? lastPoll = dbContext
                .PriceSnapshots.OrderByDescending(p => p.Id)
                .Select(p => (DateTimeOffset?)p.TimestampUtc)
                .FirstOrDefault();
            DateTimeOffset? lastSync = dbContext.Accounts.AsNoTracking()
                .Select(a => (DateTimeOffset?)a.LastSyncedUtc).AsEnumerable().Max();
            DateTimeOffset? lastWrite = lastPoll > lastSync || lastSync is null ? lastPoll : lastSync;

            return new DashboardCounts(items, recipes, tradeable, currencies, priceSnapshots, lastWrite);
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

    /// <summary>Exact last-poll timestamp, shown as the tooltip beside the relative text; null until loaded.</summary>
    public string? WorkerUpdatedTooltip
    {
        get => _workerUpdatedTooltip;
        private set => SetProperty(ref _workerUpdatedTooltip, value);
    }

    // --- App ---
    public string AppVersion { get; }

    public string AppEnvironment { get; }

    /// <summary>On-disk size of the SQLite database (+ WAL).</summary>
    public string DatabaseSize { get; }

    /// <summary>True when an update has been downloaded and will apply on the next restart.</summary>
    public bool UpdateReady
    {
        get => _updateReady;
        private set => SetProperty(ref _updateReady, value);
    }

    /// <summary>"Update X.Y.Z ready", or empty.</summary>
    public string UpdateText
    {
        get => _updateText;
        private set => SetProperty(ref _updateText, value);
    }

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

    /// <summary>Exact last-sync timestamp for the account card's tooltip; null (no tooltip) until loaded.</summary>
    public string? AccountSyncedTooltip
    {
        get => _accountSyncedTooltip;
        private set => SetProperty(ref _accountSyncedTooltip, value);
    }

    public int CharacterCount
    {
        get => _characterCount;
        private set => SetProperty(ref _characterCount, value);
    }

    /// <summary>The account's coin balance in copper (formatted as g/s/c in the view).</summary>
    private AccountValueSummary? _accountValue;

    public long CoinBalance
    {
        get => _coinBalance;
        private set => SetProperty(ref _coinBalance, value);
    }

    // --- Account value ---
    /// <summary>The account's latest value and its change over the last day and week; null before the first point.</summary>
    public AccountValueSummary? AccountValue
    {
        get => _accountValue;
        private set
        {
            if (SetProperty(ref _accountValue, value))
            {
                OnPropertyChanged(nameof(HasAccountValue));
            }
        }
    }

    public bool HasAccountValue => _accountValue is not null;

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

    /// <summary>Exact last-played time for the session card's tooltip; null while playing or with no sessions.</summary>
    public string? SessionStatusTooltip
    {
        get => _sessionStatusTooltip;
        private set => SetProperty(ref _sessionStatusTooltip, value);
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
    DateTimeOffset? LastWriteUtc
);
