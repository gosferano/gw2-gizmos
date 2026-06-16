using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly List<GameSessionRow> _all = new();
    private string _status = "";
    private int _selectedSortIndex;
    private bool _descending = true;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    public SessionsViewModel(AccountReader reader, SelectedAccountService account, SelectedSessionService selected)
    {
        _selected = selected;
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
            _all.AddRange(sessions);

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
    private string _status = "";
    private SessionValueSummary _value = SessionValueSummary.Empty;

    public SessionViewModel(AccountReader reader, SelectedAccountService account, SelectedSessionService selected)
    {
        _selected = selected;
        Breadcrumbs = SessionBreadcrumbs.Session(selected.GameSessionTitle);
        OpenCommand = new RelayCommand<SessionSegmentRow>(Open);

        if (account.AccountId is { } accountId && selected.GameSessionId is { } sessionId)
        {
            _ = LoadAsync(reader, accountId, sessionId);
        }
    }

    public ObservableCollection<SessionSegmentRow> Segments { get; } = new();

    /// <summary>The whole session's aggregated loot, shown under the character cards.</summary>
    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

    public ObservableCollection<SessionLootItem> Items { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    /// <summary>The session's estimated worth (coin + instant-sell loot) and gold-per-hour, shown as a summary row.</summary>
    public SessionValueSummary Value
    {
        get => _value;
        private set => SetProperty(ref _value, value);
    }

    public bool HasSegments => Segments.Count > 0;

    public bool HasCurrencies => Currencies.Count > 0;

    public bool HasItems => Items.Count > 0;

    /// <summary>True once loaded with no recorded changes — shows the "no loot" note under the cards.</summary>
    public bool HasNoLoot => Currencies.Count == 0 && Items.Count == 0;

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public RelayCommand<SessionSegmentRow> OpenCommand { get; }

    private async Task LoadAsync(AccountReader reader, string accountId, long sessionId)
    {
        try
        {
            List<SessionSegmentRow> segments = await Task.Run(() => reader.GetSessionSegments(accountId, sessionId));
            foreach (SessionSegmentRow segment in segments)
            {
                Segments.Add(segment);
            }

            OnPropertyChanged(nameof(HasSegments));
            Status = segments.Count == 0 ? "No characters recorded for this session." : "";

            Value = await Task.Run(() => reader.GetSessionValue(accountId, sessionId));

            SessionLoot loot = await Task.Run(() => reader.GetGameSessionLoot(accountId, sessionId));
            foreach (SessionLootCurrency currency in loot.Currencies)
            {
                Currencies.Add(currency);
            }

            foreach (SessionLootItem item in loot.Items)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(HasCurrencies));
            OnPropertyChanged(nameof(HasItems));
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

        // Include the start time so two stretches of the same character get distinct breadcrumbs.
        _selected.SelectSegment(segment.Id, $"{segment.CharacterName} ({segment.StartedAtUtc.LocalDateTime:HH:mm})");
        App.NavigateTo(typeof(SessionLootPage));
    }
}

/// <summary>Backs a segment's loot page: the currencies and items gained/lost while that character was active.</summary>
public sealed class SessionLootViewModel : ViewModelBase
{
    private bool _isEmpty;

    public SessionLootViewModel(AccountReader reader, SelectedAccountService account, SelectedSessionService selected)
    {
        Breadcrumbs = SessionBreadcrumbs.Segment(selected.GameSessionTitle, selected.SegmentTitle);

        if (account.AccountId is { } accountId && selected.SegmentId is { } segmentId)
        {
            _ = LoadAsync(reader, accountId, segmentId);
        }
    }

    public ObservableCollection<SessionLootCurrency> Currencies { get; } = new();

    public ObservableCollection<SessionLootItem> Items { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasCurrencies => Currencies.Count > 0;

    public bool HasItems => Items.Count > 0;

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

            foreach (SessionLootItem item in loot.Items)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(HasCurrencies));
            OnPropertyChanged(nameof(HasItems));
            IsEmpty = loot.IsEmpty;
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}
