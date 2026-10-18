using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the WvW page: the selected account's matchup — the three teams' standings and the skirmish under way — over
/// the Mists map with every objective in its holder's colour, refreshed while the page is open, and the character
/// playing when they're on a WvW map. Opening the page there glides to the character and follows them until the map is
/// dragged. Clicking an objective copies its chat link. A singleton, so the map stays where it was left between visits.
/// </summary>
public sealed class WvwViewModel : ViewModelBase, INavigationAware
{
    /// <summary>How often the matchup is fetched again while the page is open.</summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(15);

    /// <summary>The middle of the WvW maps on the Mists, at a zoom that fills a large window with them.</summary>
    private const double HomeX = 10500;
    private const double HomeY = 12200;
    private const double HomeZoom = 4.5;

    /// <summary>The zoom selecting a map glides to: the one map filling the view.</summary>
    private const double MapZoom = 5.5;

    private static readonly TimeSpan CopiedMessageDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// A live match's data changes every refresh or two; this long without a change means it's stuck.
    /// </summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    /// <summary>How often the character's position is read while the page is open.</summary>
    private static readonly TimeSpan PlayerPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Following the character from further out than this zooms in to it.</summary>
    private const double FollowMinZoom = 4.5;

    /// <summary>The zoom a flip in the feed glides to, close enough to see the objective's surroundings.</summary>
    private const double FlipZoom = 5.5;

    private readonly WvwMatchupProvider _provider;
    private readonly WvwActivity _activity = new();
    private int? _feedMapId;
    private readonly IClipboardService _clipboard;
    private readonly LiveGameMonitor _liveGame;
    private readonly IUiTimer _playerPoll;
    private MapPlayer? _player;
    private bool _isFollowing;
    private readonly IUiTimer _copiedTimer;
    private string? _copiedMessage;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUiTimer _refresh;
    private readonly IUiTimer _clock;
    private double _centerX = HomeX;
    private double _centerY = HomeY;
    private double _zoom = HomeZoom;
    private MapFlight? _flight;
    private WvwMatchup? _matchup;
    private string? _problem;
    private bool _isLoading;
    private string _skirmishText = "";
    private DateTimeOffset? _dataChangedAt;
    private string _dataAgeText = "";
    private bool _isDataStale;
    private MapLandmarks _landmarks = MapLandmarks.Empty;

    public WvwViewModel(
        WvwMatchupProvider provider,
        IClipboardService clipboard,
        LiveGameMonitor liveGame,
        IUiDispatcher dispatcher
    )
    {
        _provider = provider;
        _clipboard = clipboard;
        _liveGame = liveGame;
        _playerPoll = dispatcher.CreateTimer(PlayerPollInterval, PollPlayer);
        FollowPlayerCommand = new RelayCommand(StartFollowing, () => Player is not null);
        _copiedTimer = dispatcher.CreateTimer(CopiedMessageDuration, HideCopiedMessage);
        CopyChatLinkCommand = new RelayCommand<object>(target => _ = CopyAsync(target));
        _dispatcher = dispatcher;
        _refresh = dispatcher.CreateTimer(RefreshInterval, () => _ = LoadAsync());
        _clock = dispatcher.CreateTimer(
            TimeSpan.FromSeconds(1),
            () =>
            {
                UpdateSkirmishText();
                RefreshPanel();
            }
        );
        SelectMapCommand = new RelayCommand<WvwMapStatsRow>(SelectMap);
        ShowAllMapsCommand = new RelayCommand(() => SelectMap(null));
        FlyToFlipCommand = new RelayCommand<WvwFlipRow>(FlyToFlip);
        ZoomInCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Min(Zoom + 1, MaxZoom)));
        ZoomOutCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Max(Zoom - 1, MinZoom)));
        HomeCommand = new RelayCommand(() =>
        {
            IsFollowing = false;
            FlyTo(HomeX, HomeY, HomeZoom);
        });
    }

    public MapContinent Continent { get; } = MapContinent.Mists;

    public double MinZoom => 1;

    public double MaxZoom => Continent.MaxTileZoom + 1;

    public double CenterX
    {
        get => _centerX;
        set => SetProperty(ref _centerX, value);
    }

    public double CenterY
    {
        get => _centerY;
        set => SetProperty(ref _centerY, value);
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    /// <summary>The latest move the map should glide to.</summary>
    public MapFlight? Flight
    {
        get => _flight;
        private set => SetProperty(ref _flight, value);
    }

    /// <summary>The matchup; null until loaded, or when there's none to show (see <see cref="Problem"/>).</summary>
    public WvwMatchup? Matchup
    {
        get => _matchup;
        private set
        {
            if (SetProperty(ref _matchup, value))
            {
                OnPropertyChanged(nameof(Teams));
                OnPropertyChanged(nameof(Objectives));
                OnPropertyChanged(nameof(HasMatchup));
                UpdateSkirmishText();
            }
        }
    }

    public bool HasMatchup => Matchup is not null;

    /// <summary>The character in game, when they're on a WvW map; null otherwise.</summary>
    public MapPlayer? Player
    {
        get => _player;
        private set
        {
            if (SetProperty(ref _player, value))
            {
                ((RelayCommand)FollowPlayerCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>The map keeps the character in the middle; dragging the map turns this off.</summary>
    public bool IsFollowing
    {
        get => _isFollowing;
        set => SetProperty(ref _isFollowing, value);
    }

    /// <summary>Glides to the character and follows them.</summary>
    public ICommand FollowPlayerCommand { get; }

    /// <summary>The match's objectives, each with its holder.</summary>
    public IReadOnlyList<WvwObjectiveMarker> Objectives => Matchup?.Objectives ?? [];

    /// <summary>What was just copied, shown for a few seconds after a click; null otherwise.</summary>
    public string? CopiedMessage
    {
        get => _copiedMessage;
        private set
        {
            if (SetProperty(ref _copiedMessage, value))
            {
                OnPropertyChanged(nameof(HasCopiedMessage));
            }
        }
    }

    public bool HasCopiedMessage => CopiedMessage is not null;

    /// <summary>Copies a clicked objective's chat link.</summary>
    public ICommand CopyChatLinkCommand { get; }

    /// <summary>The three teams, red, blue and green.</summary>
    public IReadOnlyList<WvwTeamStanding> Teams => Matchup?.Teams ?? [];

    /// <summary>Each map's activity: kills and deaths per minute, flips lately, objectives the team can take.</summary>
    public ObservableCollection<WvwMapStatsRow> MapStats { get; } = [];

    /// <summary>The flips seen, newest first, on the selected map or on all of them.</summary>
    public ObservableCollection<WvwFlipRow> Feed { get; } = [];

    /// <summary>E.g. "All maps" or "Red Borderlands": what the feed shows.</summary>
    public string FeedScope =>
        _feedMapId is { } mapId ? MapStats.FirstOrDefault(r => r.Map.MapId == mapId)?.Name ?? "" : "All maps";

    public bool IsFeedFiltered => _feedMapId is not null;

    public bool HasNoFlips => Feed.Count == 0;

    /// <summary>
    /// Selects a map: filters the feed to it and glides to it; selecting it again goes back to all maps.
    /// </summary>
    public ICommand SelectMapCommand { get; }

    /// <summary>Back to all maps, in the feed and on the map.</summary>
    public ICommand ShowAllMapsCommand { get; }

    /// <summary>Glides the map to a flipped objective.</summary>
    public ICommand FlyToFlipCommand { get; }

    /// <summary>
    /// E.g. "Data from 12 s ago": since the match's data last changed, which a live match does almost every refresh.
    /// Until a change is seen, it's the age of the newest flip or claim. The API can serve a match frozen for long
    /// spells, and this shows it.
    /// </summary>
    public string DataAgeText
    {
        get => _dataAgeText;
        private set => SetProperty(ref _dataAgeText, value);
    }

    /// <summary>The match's data hasn't changed for longer than <see cref="StaleAfter"/>.</summary>
    public bool IsDataStale
    {
        get => _isDataStale;
        private set => SetProperty(ref _isDataStale, value);
    }

    /// <summary>E.g. "Skirmish 12 · ends in 1h 05m".</summary>
    public string SkirmishText
    {
        get => _skirmishText;
        private set => SetProperty(ref _skirmishText, value);
    }

    /// <summary>The maps' names written on the Mists.</summary>
    public MapLandmarks Landmarks
    {
        get => _landmarks;
        private set => SetProperty(ref _landmarks, value);
    }

    /// <summary>Why there's no matchup to show; null when there is one (or it's still loading).</summary>
    public string? Problem
    {
        get => _problem;
        private set
        {
            if (SetProperty(ref _problem, value))
            {
                OnPropertyChanged(nameof(HasProblem));
            }
        }
    }

    public bool HasProblem => Problem is not null;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }

    /// <summary>Back to all four maps.</summary>
    public ICommand HomeCommand { get; }

    public void OnNavigatedTo()
    {
        _ = LoadAsync();
        _refresh.Start();
        _clock.Start();
        PollPlayer();
        if (Player is not null)
        {
            StartFollowing();
        }

        _playerPoll.Start();
    }

    public void OnNavigatedFrom()
    {
        _refresh.Stop();
        _clock.Stop();
        _playerPoll.Stop();
        // The page's next map mustn't be sent the last glide again when it's bound.
        Flight = null;
    }

    private void PollPlayer()
    {
        Player = MapPlayer.On(Continent, _liveGame.Read());
        if (Player is null)
        {
            IsFollowing = false;
        }
    }

    private void StartFollowing()
    {
        if (Player is not { } player)
        {
            return;
        }

        IsFollowing = true;
        FlyTo(player.X, player.Y, Math.Max(Zoom, FollowMinZoom));
    }

    private async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = Matchup is null;
        WvwMatchupResult result = await _provider.LoadAsync().ConfigureAwait(false);
        _dispatcher.Post(() =>
        {
            IsLoading = false;
            // A failed refresh keeps the matchup already shown; only say what's wrong when there's nothing to show.
            if (result.Matchup is { } matchup)
            {
                Problem = null;
                if (Matchup is { } before && before.MatchId == matchup.MatchId)
                {
                    if (before.DataStamp != matchup.DataStamp)
                    {
                        _dataChangedAt = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    // A match first seen: its data is at least as old as its newest flip or claim.
                    _dataChangedAt = null;
                }

                bool mapsChanged =
                    Matchup is null
                    || !Matchup.Maps.Select(m => m.MapId).SequenceEqual(matchup.Maps.Select(m => m.MapId));
                Matchup = matchup;
                _activity.Add(matchup, DateTimeOffset.UtcNow);
                if (mapsChanged)
                {
                    ShowMaps(matchup.Maps);
                }

                RebuildFeed();
                RefreshPanel();
            }
            else if (Matchup is null)
            {
                Problem = result.Problem;
            }
        });
    }

    private void ShowMaps(IReadOnlyList<WvwMatchMap> maps)
    {
        MapStats.ReplaceWith(maps.Select(map => new WvwMapStatsRow(map) { IsSelected = map.MapId == _feedMapId }));
        Landmarks = new MapLandmarks(
            maps.Select(map => new MapAreaLabel(
                    MapAreaLabelKind.Map,
                    map.Name,
                    (map.MinX + map.MaxX) / 2,
                    (map.MinY + map.MaxY) / 2
                ))
                .ToList(),
            []
        );
    }

    /// <summary>
    /// Selects a map (or all maps): the feed shows its flips and the view glides to it, or back to all of them.
    /// </summary>
    private void SelectMap(WvwMapStatsRow? row)
    {
        _feedMapId = row is null || row.Map.MapId == _feedMapId ? null : row.Map.MapId;
        IsFollowing = false;
        if (_feedMapId is not null && row is not null)
        {
            WvwMatchMap map = row.Map;
            FlyTo((map.MinX + map.MaxX) / 2, (map.MinY + map.MaxY) / 2, MapZoom);
        }
        else
        {
            FlyTo(HomeX, HomeY, HomeZoom);
        }

        foreach (WvwMapStatsRow each in MapStats)
        {
            each.IsSelected = each.Map.MapId == _feedMapId;
        }

        OnPropertyChanged(nameof(FeedScope));
        OnPropertyChanged(nameof(IsFeedFiltered));
        RebuildFeed();
    }

    /// <summary>Shows the flips on the selected map (or all), keeping the rows already shown.</summary>
    private void RebuildFeed()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Feed.ReplaceWith(
            _activity.Feed.Where(f => _feedMapId is null || f.MapId == _feedMapId).Select(f => new WvwFlipRow(f)),
            row => row.Flip,
            (_, _) => { }
        );
        foreach (WvwFlipRow row in Feed)
        {
            row.Refresh(now);
        }

        OnPropertyChanged(nameof(HasNoFlips));
    }

    /// <summary>Brings the maps' numbers and the feed's times up to date (each second: protection runs out).</summary>
    private void RefreshPanel()
    {
        if (Matchup is not { } matchup)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (WvwMapStatsRow row in MapStats)
        {
            row.Refresh(matchup, _activity, now);
        }

        foreach (WvwFlipRow row in Feed)
        {
            row.Refresh(now);
        }
    }

    private void FlyToFlip(WvwFlipRow? row)
    {
        if (row is not null && Matchup?.Objectives.FirstOrDefault(o => o.Id == row.Flip.ObjectiveId) is { } objective)
        {
            IsFollowing = false;
            FlyTo(objective.X, objective.Y, Math.Max(Zoom, FlipZoom));
        }
    }

    private void UpdateSkirmishText()
    {
        if (Matchup is not { } matchup)
        {
            SkirmishText = "";
            return;
        }

        TimeSpan left = matchup.SkirmishEndsAt - DateTimeOffset.UtcNow;
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }

        string remaining =
            left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes:00}m" : $"{left.Minutes}:{left.Seconds:00}";
        SkirmishText = $"Skirmish {matchup.Skirmish} · ends in {remaining}";

        DateTimeOffset dataFrom = _dataChangedAt ?? matchup.NewestEvent;
        TimeSpan age = DateTimeOffset.UtcNow - dataFrom;
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        IsDataStale = age > StaleAfter;
        string ago =
            age < TimeSpan.FromMinutes(1) ? $"{(int)age.TotalSeconds} s ago"
            : age < TimeSpan.FromHours(1) ? $"{(int)age.TotalMinutes} min ago"
            : $"{(int)age.TotalHours}h {age.Minutes:00}m ago";
        DataAgeText = IsDataStale ? $"Data from {ago} · the API may be stuck" : $"Data from {ago}";
    }

    private void FlyTo(double x, double y, double zoom) => Flight = new MapFlight(x, y, zoom);

    private async Task CopyAsync(object? target)
    {
        if (target is not WvwObjectiveMarker objective)
        {
            return;
        }

        try
        {
            await _clipboard.SetTextAsync(objective.ChatLink);
        }
        catch (Exception)
        {
            // The clipboard can be briefly unavailable; nothing copied, nothing to report.
            return;
        }

        CopiedMessage = $"Copied {objective.Name} {objective.ChatLink}";
        _copiedTimer.Stop();
        _copiedTimer.Start();
    }

    private void HideCopiedMessage()
    {
        _copiedTimer.Stop();
        CopiedMessage = null;
    }
}
