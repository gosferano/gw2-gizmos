using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Sessions hub: a card per play session of the selected account, each opening that session's character
/// timeline. The full list is held and a sorted/date-filtered view is shown (sort by date, profit, or profit/hour,
/// either direction). Transient, so it reloads on navigation. Empty until the (off-by-default, Windows-only) Play
/// sessions feature has recorded a session.
/// </summary>
public sealed class SessionsViewModel : ViewModelBase
{
    private readonly SelectedSessionService _selected;
    private readonly SessionDeletionTracker _deleted;
    private readonly List<GameSessionRow> _all = new();
    private string _status = "";
    private int _selectedSortIndex;
    private bool _descending = true;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    public SessionsViewModel(
        AccountReader reader, SelectedAccountService account, SelectedSessionService selected, SessionDeletionTracker deleted)
    {
        _selected = selected;
        _deleted = deleted;
        HasAccount = !string.IsNullOrEmpty(account.AccountId);
        OpenCommand = new RelayCommand<GameSessionRow>(Open);
        ToggleDirectionCommand = new RelayCommand(() => Descending = !Descending);
        ClearFilterCommand = new RelayCommand(ClearFilter);

        if (account.AccountId is { } accountId)
        {
            _ = LoadAsync(reader, accountId);
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

    private async Task LoadAsync(AccountReader reader, string accountId)
    {
        try
        {
            List<GameSessionRow> sessions = await Task.Run(() => reader.GetGameSessions(accountId));
            _all.Clear();
            // Hide any sitting deleted this run; the worker clears it from the DB a few seconds later.
            _all.AddRange(sessions.Where(s => !_deleted.IsSessionDeleted(s.Id)));

            OnPropertyChanged(nameof(HasAnySessions));
            Status = _all.Count == 0
                ? "No play sessions recorded yet. Enable “Play sessions” in Settings (Windows only) and launch Guild Wars 2."
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
        App.NavigateTo(typeof(SessionPage));
    }
}

/// <summary>Backs a single session's page: its ordered character segments (the switches within the sitting).</summary>
public sealed class SessionViewModel : ViewModelBase
{
    private readonly SelectedSessionService _selected;
    private readonly DeleteRequestStore _deletes;
    private readonly SessionDeletionTracker _deleted;
    private readonly string? _accountId;
    private readonly long? _sessionId;
    private string _status = "";
    private SessionValueSummary _value = SessionValueSummary.Empty;

    public SessionViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedSessionService selected,
        DeleteRequestStore deletes,
        SessionDeletionTracker deleted)
    {
        _selected = selected;
        _deletes = deletes;
        _deleted = deleted;
        _accountId = account.AccountId;
        _sessionId = selected.GameSessionId;
        Breadcrumbs = SessionBreadcrumbs.Session(selected.GameSessionTitle);
        OpenCommand = new RelayCommand<SessionSegmentRow>(Open);
        DeleteSessionCommand = new RelayCommand(DeleteSession);

        if (account.AccountId is { } accountId && selected.GameSessionId is { } sessionId)
        {
            _ = LoadAsync(reader, accountId, sessionId);
        }
    }

    public ObservableCollection<SessionSegmentRow> Segments { get; } = new();

    /// <summary>The whole session's aggregated loot, shown under the character cards.</summary>
    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

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

    private async Task LoadAsync(AccountReader reader, string accountId, long sessionId)
    {
        try
        {
            List<SessionSegmentRow> segments = await Task.Run(() => reader.GetSessionSegments(accountId, sessionId));
            // Drop any segment deleted this run. If that empties the sitting, its last segment was deleted on the
            // loot page (the worker also removes the now-empty session) — there's nothing to show, so go back to the
            // list, marking the session deleted so it's hidden there too until the worker catches up.
            List<SessionSegmentRow> visible = segments.Where(s => !_deleted.IsSegmentDeleted(s.Id)).ToList();
            if (visible.Count == 0 && segments.Count > 0)
            {
                _deleted.MarkSession(sessionId);
                App.NavigateTo(typeof(SessionsPage));
                return;
            }

            foreach (SessionSegmentRow segment in visible)
            {
                Segments.Add(segment);
            }

            OnPropertyChanged(nameof(HasSegments));
            OnPropertyChanged(nameof(CanDeleteSession));
            Status = Segments.Count == 0 ? "No characters recorded for this session." : "";

            Value = await Task.Run(() => reader.GetSessionValue(accountId, sessionId));

            SessionLoot loot = await Task.Run(() => reader.GetGameSessionLoot(accountId, sessionId));
            foreach (SessionLootCurrency currency in loot.Currencies)
            {
                Currencies.Add(currency);
            }

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
        App.NavigateTo(typeof(SessionLootPage));
    }

    private void DeleteSession()
    {
        // The active sitting is still being written by the tracker; its button is hidden, but guard here too.
        if (_accountId is not { } accountId || _sessionId is not { } sessionId || !CanDeleteSession)
        {
            return;
        }

        if (!SessionDeletePrompt.ConfirmSession())
        {
            return;
        }

        _deleted.MarkSession(sessionId);
        _deletes.Enqueue(DeletableData.Session, accountId, sessionId);
        App.NavigateTo(typeof(SessionsPage));
    }
}

/// <summary>Backs a segment's loot page: the currencies and items gained/lost while that character was active.</summary>
public sealed class SessionLootViewModel : ViewModelBase
{
    private readonly DeleteRequestStore _deletes;
    private readonly SessionDeletionTracker _deleted;
    private readonly string? _accountId;
    private readonly long? _segmentId;
    private bool _isEmpty;

    public SessionLootViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedSessionService selected,
        DeleteRequestStore deletes,
        SessionDeletionTracker deleted)
    {
        _deletes = deletes;
        _deleted = deleted;
        _accountId = account.AccountId;
        _segmentId = selected.SegmentId;
        Breadcrumbs = SessionBreadcrumbs.Segment(selected.GameSessionTitle, selected.SegmentTitle);
        DeleteSegmentCommand = new RelayCommand(DeleteSegment);
        // The active (in-progress) segment is still being written, so it can't be deleted.
        CanDelete = account.AccountId is not null && selected.SegmentId is not null && !selected.SegmentIsActive;

        if (account.AccountId is { } accountId && selected.SegmentId is { } segmentId)
        {
            _ = LoadAsync(reader, accountId, segmentId);
        }
    }

    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

    /// <summary>The segment's item loot, sortable by gold value or quantity (asc/desc).</summary>
    public LootItemsSorter ItemsSorter { get; } = new();

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

    private async Task LoadAsync(AccountReader reader, string accountId, long segmentId)
    {
        try
        {
            SessionLoot loot = await Task.Run(() => reader.GetSegmentLoot(accountId, segmentId));
            foreach (SessionLootCurrency currency in loot.Currencies)
            {
                Currencies.Add(currency);
            }

            ItemsSorter.SetItems(loot.Items);

            OnPropertyChanged(nameof(HasCurrencies));
            IsEmpty = loot.IsEmpty;
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    private void DeleteSegment()
    {
        if (_accountId is not { } accountId || _segmentId is not { } segmentId || !CanDelete)
        {
            return;
        }

        if (!SessionDeletePrompt.ConfirmSegment())
        {
            return;
        }

        // Back to the session, which re-reads without this segment (and bounces to the list if it was the last one).
        _deleted.MarkSegment(segmentId);
        _deletes.Enqueue(DeletableData.SessionSegment, accountId, segmentId);
        App.NavigateTo(typeof(SessionPage));
    }
}

/// <summary>The delete confirmations for a play session and a character segment — both permanent (session history
/// isn't re-synced), so they warn before the worker clears the rows.</summary>
internal static class SessionDeletePrompt
{
    public static bool ConfirmSession() => Confirm(
        "Delete this play session?",
        "This permanently removes the recorded sitting and all its character segments. Session history doesn't re-sync.");

    public static bool ConfirmSegment() => Confirm(
        "Delete this character segment?",
        "This permanently removes the recorded stretch. The session totals are unchanged, but the figures on the "
            + "neighbouring segments may shift to cover the gap. Deleting the last segment removes the session too.");

    private static bool Confirm(string title, string detail) =>
        MessageBox.Show($"{title}\n\n{detail}", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning)
        == MessageBoxResult.Yes;
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
