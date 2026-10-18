using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the World map page: Tyria's map, its labels and markers, where it's looking, and the character playing, live
/// from MumbleLink while the page is open. Opening the page while in game follows the character until the map is
/// dragged away. Clicking a landmark with a chat link copies it. A singleton, so the map stays where it was left when
/// coming back to the page.
/// </summary>
public sealed class WorldMapViewModel : ViewModelBase, INavigationAware
{
    /// <summary>How often the character's position is read while the page is open.</summary>
    private static readonly TimeSpan PlayerPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Following the character from further out than this zooms in to it, to map level.</summary>
    private const double FollowMinZoom = 4;
    private const double FollowZoom = 5;

    /// <summary>Lion's Arch, at a zoom that shows central Tyria around it.</summary>
    private const double HomeX = 49216;
    private const double HomeY = 31488;
    private const double HomeZoom = 3;

    private static readonly TimeSpan CopiedMessageDuration = TimeSpan.FromSeconds(3);

    private readonly IClipboardService _clipboard;
    private readonly LiveGameMonitor _liveGame;
    private readonly IUiTimer _copiedTimer;
    private readonly IUiTimer _playerPoll;
    private MapPlayer? _player;
    private bool _isFollowing;
    private MapFlight? _flight;
    private double _centerX = HomeX;
    private double _centerY = HomeY;
    private double _zoom = HomeZoom;
    private MapLandmarks _landmarks = MapLandmarks.Empty;
    private string? _copiedMessage;

    public WorldMapViewModel(
        MapLandmarkProvider landmarks,
        IClipboardService clipboard,
        LiveGameMonitor liveGame,
        IUiDispatcher dispatcher
    )
    {
        _clipboard = clipboard;
        _liveGame = liveGame;
        _copiedTimer = dispatcher.CreateTimer(CopiedMessageDuration, HideCopiedMessage);
        _playerPoll = dispatcher.CreateTimer(PlayerPollInterval, PollPlayer);
        FollowPlayerCommand = new RelayCommand(StartFollowing, () => Player is not null);
        ZoomInCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Min(Zoom + 1, MaxZoom)));
        ZoomOutCommand = new RelayCommand(() => FlyTo(CenterX, CenterY, Math.Max(Zoom - 1, MinZoom)));
        HomeCommand = new RelayCommand(GoHome);
        CopyChatLinkCommand = new RelayCommand<MapMarker>(marker => _ = CopyAsync(marker));
        _ = LoadAsync(landmarks, dispatcher);
    }

    public MapContinent Continent { get; } = MapContinent.Tyria;

    /// <summary>Far enough out to see all of Tyria in a normal window.</summary>
    public double MinZoom => 1;

    /// <summary>One step past the tiles' native detail, which the map upscales.</summary>
    public double MaxZoom => Continent.MaxTileZoom + 1;

    /// <summary>The continent point at the middle of the map.</summary>
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

    /// <summary>The zoom level, fractional between the tile levels.</summary>
    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    /// <summary>The map's region, map and sector names and its markers; empty until loaded.</summary>
    public MapLandmarks Landmarks
    {
        get => _landmarks;
        private set => SetProperty(ref _landmarks, value);
    }

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

    /// <summary>
    /// The character in game, where they are and which way they face; null when not in the world on Tyria.
    /// </summary>
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

    /// <summary>The latest move the map should glide to rather than jump (each request a new instance).</summary>
    public MapFlight? Flight
    {
        get => _flight;
        private set => SetProperty(ref _flight, value);
    }

    /// <summary>Centres the map on the character and follows them.</summary>
    public ICommand FollowPlayerCommand { get; }

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }

    /// <summary>Back to central Tyria.</summary>
    public ICommand HomeCommand { get; }

    /// <summary>Copies a clicked <see cref="MapMarker"/>'s chat link.</summary>
    public ICommand CopyChatLinkCommand { get; }

    public void OnNavigatedTo()
    {
        PollPlayer();
        if (Player is not null)
        {
            StartFollowing();
        }

        _playerPoll.Start();
    }

    public void OnNavigatedFrom() => _playerPoll.Stop();

    private void PollPlayer()
    {
        LiveGameState game = _liveGame.Read();
        Player =
            game.Position is { } at && game.Character is { } name ? new MapPlayer(name, at.X, at.Y, at.Heading) : null;
        if (Player is null)
        {
            IsFollowing = false;
        }
    }

    /// <summary>Glides to the character and follows them (the map keeps them in the middle from then on).</summary>
    private void StartFollowing()
    {
        if (Player is not { } player)
        {
            return;
        }

        IsFollowing = true;
        FlyTo(player.X, player.Y, Zoom < FollowMinZoom ? FollowZoom : Zoom);
    }

    private void FlyTo(double x, double y, double zoom) => Flight = new MapFlight(x, y, zoom);

    private async Task LoadAsync(MapLandmarkProvider provider, IUiDispatcher dispatcher)
    {
        MapLandmarks landmarks = await provider.LoadAsync(Continent).ConfigureAwait(false);
        dispatcher.Post(() => Landmarks = landmarks);
    }

    private async Task CopyAsync(MapMarker? marker)
    {
        if (marker?.ChatLink is not { Length: > 0 } chatLink)
        {
            return;
        }

        try
        {
            await _clipboard.SetTextAsync(chatLink);
        }
        catch (Exception)
        {
            // The clipboard can be briefly unavailable; nothing copied, nothing to report.
            return;
        }

        CopiedMessage = $"Copied {marker.Name} {chatLink}";
        _copiedTimer.Stop();
        _copiedTimer.Start();
    }

    private void HideCopiedMessage()
    {
        _copiedTimer.Stop();
        CopiedMessage = null;
    }

    private void GoHome()
    {
        IsFollowing = false;
        FlyTo(HomeX, HomeY, HomeZoom);
    }
}
