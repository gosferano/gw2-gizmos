using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the WvW page: the selected account's matchup — the three teams' standings and the skirmish under way — over
/// the Mists map with every objective in its holder's colour, refreshed while the page is open. Clicking an objective
/// copies its chat link. A singleton, so the map stays where it was left between visits.
/// </summary>
public sealed class WvwViewModel : ViewModelBase, INavigationAware
{
    /// <summary>How often the matchup is fetched again while the page is open.</summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    /// <summary>The middle of the WvW maps on the Mists, at a zoom that fills a large window with them.</summary>
    private const double HomeX = 10500;
    private const double HomeY = 12200;
    private const double HomeZoom = 4.5;

    private static readonly TimeSpan CopiedMessageDuration = TimeSpan.FromSeconds(3);

    private readonly WvwMatchupProvider _provider;
    private readonly IClipboardService _clipboard;
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
    private MapLandmarks _landmarks = MapLandmarks.Empty;

    public WvwViewModel(WvwMatchupProvider provider, IClipboardService clipboard, IUiDispatcher dispatcher)
    {
        _provider = provider;
        _clipboard = clipboard;
        _copiedTimer = dispatcher.CreateTimer(CopiedMessageDuration, HideCopiedMessage);
        CopyChatLinkCommand = new RelayCommand<object>(target => _ = CopyAsync(target));
        _dispatcher = dispatcher;
        _refresh = dispatcher.CreateTimer(RefreshInterval, () => _ = LoadAsync());
        _clock = dispatcher.CreateTimer(TimeSpan.FromSeconds(1), UpdateSkirmishText);
        ZoomInCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Min(Zoom + 1, MaxZoom)));
        ZoomOutCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Max(Zoom - 1, MinZoom)));
        HomeCommand = new RelayCommand(() => FlyTo(HomeX, HomeY, HomeZoom));
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
    }

    public void OnNavigatedFrom()
    {
        _refresh.Stop();
        _clock.Stop();
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
                bool mapsChanged =
                    Matchup is null
                    || !Matchup.Maps.Select(m => m.MapId).SequenceEqual(matchup.Maps.Select(m => m.MapId));
                Matchup = matchup;
                if (mapsChanged)
                {
                    ShowMaps(matchup.Maps);
                }
            }
            else if (Matchup is null)
            {
                Problem = result.Problem;
            }
        });
    }

    private void ShowMaps(IReadOnlyList<WvwMatchMap> maps)
    {
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
