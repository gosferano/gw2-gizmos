using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Sessions hub: a card per play session of the selected account, each opening that session's character
/// timeline. The full list is held and a sorted/date-filtered view is shown (sort by date, profit, or profit/hour,
/// either direction). Refreshes in place as sessions start/end and loot syncs. Empty until the (off-by-default) Play
/// sessions feature has recorded a session.
/// </summary>
public sealed class SessionsViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly SelectedSessionService _selected;
    private readonly SessionDeletionTracker _deleted;
    private readonly INavigationService _navigation;
    private readonly List<GameSessionRow> _all = new();
    private string _status = "";
    private int _selectedSortIndex;
    private bool _descending = true;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    public SessionsViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedSessionService selected,
        SessionDeletionTracker deleted,
        INavigationService navigation,
        DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = account.AccountId;
        _selected = selected;
        _deleted = deleted;
        _navigation = navigation;
        HasAccount = !string.IsNullOrEmpty(account.AccountId);
        OpenCommand = new RelayCommand<GameSessionRow>(Open);
        ToggleDirectionCommand = new RelayCommand(() => Descending = !Descending);
        ClearFilterCommand = new RelayCommand(ClearFilter);

        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(OnDataChanged);
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    private void OnDataChanged(DataChanges changes)
    {
        // Loot syncs move profit; the clock only matters while a session's running duration is shown.
        if ((changes & (DataChanges.Sessions | DataChanges.Holdings)) != 0
            || ((changes & DataChanges.Clock) != 0 && _all.Any(s => s.IsActive)))
        {
            _ = LoadAsync();
        }
    }

    /// <summary>The filtered + sorted view shown in the list.</summary>
    public ObservableCollection<GameSessionRow> Sessions { get; } = new();

    public bool HasAccount { get; }

    /// <summary>Any sessions recorded at all (drives the sort/filter controls); independent of the active filter.</summary>
    public bool HasAnySessions => _all.Count > 0;

    /// <summary>Any sessions in the current filtered view.</summary>
    public bool HasResults => Sessions.Count > 0;

    /// <summary>True when sessions exist but the date filter excludes them all.</summary>
    public bool FilteredEmpty => HasAnySessions && !HasResults;

    /// <summary>Sort fields, in combo order; index maps to the key in <see cref="ApplyView"/>.</summary>
    public IReadOnlyList<string> SortOptions { get; } = new[] { "Date", "Profit", "Profit / hour" };

    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                ApplyView();
            }
        }
    }

    public bool Descending
    {
        get => _descending;
        set
        {
            if (SetProperty(ref _descending, value))
            {
                OnPropertyChanged(nameof(DirectionLabel));
                ApplyView();
            }
        }
    }

    /// <summary>Direction button label, e.g. "Descending".</summary>
    public string DirectionLabel => Descending ? "Descending" : "Ascending";

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                ApplyView();
            }
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                ApplyView();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public RelayCommand<GameSessionRow> OpenCommand { get; }

    public RelayCommand ToggleDirectionCommand { get; }

    public RelayCommand ClearFilterCommand { get; }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            List<GameSessionRow> sessions = await Task.Run(() => _reader.GetGameSessions(accountId));
            _all.Clear();
            // Hide any sitting deleted this run; the worker clears it from the DB a few seconds later.
            _all.AddRange(sessions.Where(s => !_deleted.IsSessionDeleted(s.Id)));

            OnPropertyChanged(nameof(HasAnySessions));
            Status = _all.Count == 0
                ? "No play sessions recorded yet. Enable “Play sessions” in Settings and launch Guild Wars 2."
                : "";
            ApplyView();
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    /// <summary>Applies the date filter then the chosen sort, repopulating the displayed list.</summary>
    private void ApplyView()
    {
        IEnumerable<GameSessionRow> query = _all;
        if (FromDate is { } from)
        {
            query = query.Where(s => s.StartedAtUtc.LocalDateTime.Date >= from.Date);
        }

        if (ToDate is { } to)
        {
            query = query.Where(s => s.StartedAtUtc.LocalDateTime.Date <= to.Date);
        }

        Func<GameSessionRow, IComparable> key = SelectedSortIndex switch
        {
            1 => s => s.TotalValueCopper,
            2 => s => s.ProfitPerHourCopper,
            _ => s => s.StartedAtUtc,
        };
        IEnumerable<GameSessionRow> ordered = Descending ? query.OrderByDescending(key) : query.OrderBy(key);

        Sessions.Clear();
        foreach (GameSessionRow session in ordered)
        {
            Sessions.Add(session);
        }

        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(FilteredEmpty));
    }

    private void ClearFilter()
    {
        FromDate = null;
        ToDate = null;
    }

    private void Open(GameSessionRow? session)
    {
        if (session is null)
        {
            return;
        }

        _selected.SelectSession(session.Id, session.Title);
        _navigation.NavigateTo(AppPage.Session);
    }
}

/// <summary>Backs a single session's page: its ordered character segments (the switches within the sitting).</summary>
public sealed class SessionViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly IDisposable _dataChanges;
    private readonly SelectedSessionService _selected;
    private readonly DeleteRequestStore _deletes;
    private readonly SessionDeletionTracker _deleted;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly string? _accountId;
    private readonly long? _sessionId;
    private string _status = "";
    private SessionValueSummary _value = SessionValueSummary.Empty;

    public SessionViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedSessionService selected,
        DeleteRequestStore deletes,
        SessionDeletionTracker deleted,
        INavigationService navigation,
        IDialogService dialogs,
        DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _selected = selected;
        _deletes = deletes;
        _deleted = deleted;
        _navigation = navigation;
        _dialogs = dialogs;
        _accountId = account.AccountId;
        _sessionId = selected.GameSessionId;
        Breadcrumbs = SessionBreadcrumbs.Session(selected.GameSessionTitle);
        OpenCommand = new RelayCommand<SessionSegmentRow>(Open);
        DeleteSessionCommand = new RelayCommand(() => _ = DeleteSessionAsync());

        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(OnDataChanged);
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    private void OnDataChanged(DataChanges changes)
    {
        if ((changes & (DataChanges.Sessions | DataChanges.Holdings)) != 0
            || ((changes & DataChanges.Clock) != 0 && Segments.Any(s => s.IsActive)))
        {
            _ = LoadAsync();
        }
    }

    public ObservableCollection<SessionSegmentRow> Segments { get; } = new();

    /// <summary>The whole session's aggregated loot, shown under the character cards.</summary>
    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

    /// <summary>Every map the session was played on, with the time on each, most-played first.</summary>
    public ObservableCollection<MapTimeRow> Maps { get; } = new();

    public bool HasMaps => Maps.Count > 0;

    /// <summary>The aggregated item loot, sortable by gold value or quantity (asc/desc).</summary>
    public LootItemsSorter ItemsSorter { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    /// <summary>The session's estimated worth (coin + instant-sell loot) and gold-per-hour, shown as a summary row.</summary>
    public SessionValueSummary Value
    {
        get => _value;
        private set => SetProperty(ref _value, value);
    }

    public bool HasSegments => Segments.Count > 0;

    public bool HasCurrencies => Currencies.Count > 0;

    /// <summary>True once loaded with no recorded changes — shows the "no loot" note under the cards.</summary>
    public bool HasNoLoot => Currencies.Count == 0 && !ItemsSorter.HasItems;

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public RelayCommand<SessionSegmentRow> OpenCommand { get; }

    /// <summary>Deletes this whole sitting (and its segments) via the worker; shown by the page's bottom button.</summary>
    public RelayCommand DeleteSessionCommand { get; }

    /// <summary>The bottom Delete button shows only once loaded, when the sitting isn't the active (in-progress) one.</summary>
    public bool CanDeleteSession => _accountId is not null && HasSegments && !Segments.Any(s => s.IsActive);

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId || _sessionId is not { } sessionId)
        {
            return;
        }

        try
        {
            List<SessionSegmentRow> segments = await Task.Run(() => _reader.GetSessionSegments(accountId, sessionId));
            // Drop any segment deleted this run. If that empties the sitting, its last segment was deleted on the
            // loot page (the worker also removes the now-empty session) — there's nothing to show, so go back to the
            // list, marking the session deleted so it's hidden there too until the worker catches up.
            List<SessionSegmentRow> visible = segments.Where(s => !_deleted.IsSegmentDeleted(s.Id)).ToList();
            if (visible.Count == 0 && segments.Count > 0)
            {
                _deleted.MarkSession(sessionId);
                _navigation.NavigateTo(AppPage.Sessions);
                return;
            }

            // Read everything first, then swap it in at once, so a live refresh doesn't flash an empty page.
            (SessionValueSummary value, SessionLoot loot, List<MapTimeRow> maps) = await Task.Run(() =>
                (_reader.GetSessionValue(accountId, sessionId), _reader.GetGameSessionLoot(accountId, sessionId),
                    _reader.GetSessionMaps(accountId, sessionId)));

            Segments.ReplaceWith(visible);
            OnPropertyChanged(nameof(HasSegments));
            OnPropertyChanged(nameof(CanDeleteSession));
            Status = Segments.Count == 0 ? "No characters recorded for this session." : "";

            Value = value;
            Maps.ReplaceWith(maps);
            OnPropertyChanged(nameof(HasMaps));
            Currencies.ReplaceWith(loot.Currencies);
            ItemsSorter.SetItems(loot.Items);

            OnPropertyChanged(nameof(HasCurrencies));
            OnPropertyChanged(nameof(HasNoLoot));
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    private void Open(SessionSegmentRow? segment)
    {
        if (segment is null)
        {
            return;
        }

        // Include the start time so two stretches of the same character get distinct breadcrumbs. Carry whether the
        // segment is active so the loot page knows whether to offer Delete.
        _selected.SelectSegment(
            segment.Id, $"{segment.CharacterName} ({segment.StartedAtUtc.LocalDateTime:HH:mm})", segment.IsActive);
        _navigation.NavigateTo(AppPage.SessionLoot);
    }

    private async Task DeleteSessionAsync()
    {
        // The active sitting is still being written by the tracker; its button is hidden, but guard here too.
        if (_accountId is not { } accountId || _sessionId is not { } sessionId || !CanDeleteSession)
        {
            return;
        }

        if (!await SessionDeletePrompt.ConfirmSessionAsync(_dialogs))
        {
            return;
        }

        _deleted.MarkSession(sessionId);
        _deletes.Enqueue(DeletableData.Session, accountId, sessionId);
        _navigation.NavigateTo(AppPage.Sessions);
    }
}

/// <summary>Backs a segment's loot page: the currencies and items gained/lost while that character was active.</summary>
public sealed class SessionLootViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly IDisposable _dataChanges;
    private readonly DeleteRequestStore _deletes;
    private readonly SessionDeletionTracker _deleted;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly string? _accountId;
    private readonly long? _segmentId;
    private bool _isEmpty;

    public SessionLootViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedSessionService selected,
        DeleteRequestStore deletes,
        SessionDeletionTracker deleted,
        INavigationService navigation,
        IDialogService dialogs,
        DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _deletes = deletes;
        _deleted = deleted;
        _navigation = navigation;
        _dialogs = dialogs;
        _accountId = account.AccountId;
        _segmentId = selected.SegmentId;
        Breadcrumbs = SessionBreadcrumbs.Segment(selected.GameSessionTitle, selected.SegmentTitle);
        DeleteSegmentCommand = new RelayCommand(() => _ = DeleteSegmentAsync());
        // The active (in-progress) segment is still being written, so it can't be deleted.
        CanDelete = account.AccountId is not null && selected.SegmentId is not null && !selected.SegmentIsActive;

        _ = LoadAsync();
        // The segment's loot grows as syncs land, and its window closes when the character switches or the game ends.
        _dataChanges = dataChanges.Subscribe(changes =>
        {
            if ((changes & (DataChanges.Sessions | DataChanges.Holdings)) != 0)
            {
                _ = LoadAsync();
            }
        });
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

    /// <summary>The segment's item loot, sortable by gold value or quantity (asc/desc).</summary>
    public LootItemsSorter ItemsSorter { get; } = new();

    /// <summary>Every map the character played on in this segment, with the time on each, most-played first.</summary>
    public ObservableCollection<MapTimeRow> Maps { get; } = new();

    public bool HasMaps => Maps.Count > 0;

    public BreadcrumbEntry[] Breadcrumbs { get; }

    /// <summary>Deletes this segment via the worker; shown by the page's bottom button when <see cref="CanDelete"/>.</summary>
    public RelayCommand DeleteSegmentCommand { get; }

    /// <summary>Whether the bottom Delete button shows — false for the active (in-progress) segment.</summary>
    public bool CanDelete { get; }

    public bool HasCurrencies => Currencies.Count > 0;

    /// <summary>True once loaded and nothing changed — shows the "no loot recorded" note.</summary>
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId || _segmentId is not { } segmentId)
        {
            return;
        }

        try
        {
            (SessionLoot loot, List<MapTimeRow> maps) = await Task.Run(() =>
                (_reader.GetSegmentLoot(accountId, segmentId), _reader.GetSegmentMaps(accountId, segmentId)));
            Maps.ReplaceWith(maps);
            OnPropertyChanged(nameof(HasMaps));
            Currencies.ReplaceWith(loot.Currencies);
            ItemsSorter.SetItems(loot.Items);

            OnPropertyChanged(nameof(HasCurrencies));
            IsEmpty = loot.IsEmpty;
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    private async Task DeleteSegmentAsync()
    {
        if (_accountId is not { } accountId || _segmentId is not { } segmentId || !CanDelete)
        {
            return;
        }

        if (!await SessionDeletePrompt.ConfirmSegmentAsync(_dialogs))
        {
            return;
        }

        // Back to the session, which re-reads without this segment (and bounces to the list if it was the last one).
        _deleted.MarkSegment(segmentId);
        _deletes.Enqueue(DeletableData.SessionSegment, accountId, segmentId);
        _navigation.NavigateTo(AppPage.Session);
    }
}

/// <summary>The delete confirmations for a play session and a character segment — both permanent (session history
/// isn't re-synced), so they warn before the worker clears the rows.</summary>
internal static class SessionDeletePrompt
{
    public static Task<bool> ConfirmSessionAsync(IDialogService dialogs) => ConfirmAsync(
        dialogs,
        "Delete this play session?",
        "This permanently removes the recorded sitting and all its character segments. Session history doesn't re-sync.");

    public static Task<bool> ConfirmSegmentAsync(IDialogService dialogs) => ConfirmAsync(
        dialogs,
        "Delete this character segment?",
        "This permanently removes the recorded stretch. The session totals are unchanged, but the figures on the "
            + "neighbouring segments may shift to cover the gap. Deleting the last segment removes the session too.");

    private static Task<bool> ConfirmAsync(IDialogService dialogs, string title, string detail) =>
        dialogs.ConfirmAsync("Delete", $"{title}\n\n{detail}");
}

/// <summary>
/// Holds a session/segment's item loot and exposes a sorted <see cref="View"/> the UI binds to — by gold value or
/// quantity, ascending or descending, mirroring the sessions-hub sorter. Sorting is on the signed delta/value
/// (descending shows the biggest gains first; losses fall to the bottom).
/// </summary>
public sealed class LootItemsSorter : ViewModelBase
{
    private IReadOnlyList<SessionLootItem> _source = Array.Empty<SessionLootItem>();
    private int _selectedSortIndex; // 0 = gold value, 1 = quantity
    private bool _descending = true;

    public LootItemsSorter()
    {
        ToggleDirectionCommand = new RelayCommand(() => Descending = !Descending);
    }

    /// <summary>The sorted view bound by the list.</summary>
    public ObservableCollection<SessionLootItem> View { get; } = new();

    public IReadOnlyList<string> SortOptions { get; } = new[] { "Gold value", "Quantity" };

    public bool HasItems => _source.Count > 0;

    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                Resort();
            }
        }
    }

    public bool Descending
    {
        get => _descending;
        set
        {
            if (SetProperty(ref _descending, value))
            {
                OnPropertyChanged(nameof(DirectionLabel));
                Resort();
            }
        }
    }

    public string DirectionLabel => Descending ? "Descending" : "Ascending";

    public RelayCommand ToggleDirectionCommand { get; }

    /// <summary>Replaces the backing items and re-sorts (preserving the current sort + direction).</summary>
    public void SetItems(IEnumerable<SessionLootItem> items)
    {
        _source = items.ToList();
        OnPropertyChanged(nameof(HasItems));
        Resort();
    }

    private void Resort()
    {
        Func<SessionLootItem, IComparable> key = _selectedSortIndex == 1 ? i => i.Delta : i => i.ValueCopper;
        IEnumerable<SessionLootItem> ordered = _descending ? _source.OrderByDescending(key) : _source.OrderBy(key);

        View.Clear();
        foreach (SessionLootItem item in ordered)
        {
            View.Add(item);
        }
    }
}
