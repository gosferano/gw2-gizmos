using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the World map page: Tyria's map, its labels and markers, and where it's looking. Clicking a landmark with a
/// chat link copies it. A singleton, so the map stays where it was left when coming back to the page.
/// </summary>
public sealed class WorldMapViewModel : ViewModelBase
{
    /// <summary>Lion's Arch, at a zoom that shows central Tyria around it.</summary>
    private const double HomeX = 49216;
    private const double HomeY = 31488;
    private const double HomeZoom = 3;

    private static readonly TimeSpan CopiedMessageDuration = TimeSpan.FromSeconds(3);

    private readonly IClipboardService _clipboard;
    private readonly IUiTimer _copiedTimer;
    private double _centerX = HomeX;
    private double _centerY = HomeY;
    private double _zoom = HomeZoom;
    private MapLandmarks _landmarks = MapLandmarks.Empty;
    private string? _copiedMessage;

    public WorldMapViewModel(MapLandmarkProvider landmarks, IClipboardService clipboard, IUiDispatcher dispatcher)
    {
        _clipboard = clipboard;
        _copiedTimer = dispatcher.CreateTimer(CopiedMessageDuration, HideCopiedMessage);
        ZoomInCommand = new RelayCommand(() => Zoom = Math.Min(Zoom + 1, MaxZoom));
        ZoomOutCommand = new RelayCommand(() => Zoom = Math.Max(Zoom - 1, MinZoom));
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

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }

    /// <summary>Back to central Tyria.</summary>
    public ICommand HomeCommand { get; }

    /// <summary>Copies a clicked <see cref="MapMarker"/>'s chat link.</summary>
    public ICommand CopyChatLinkCommand { get; }

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
        CenterX = HomeX;
        CenterY = HomeY;
        Zoom = HomeZoom;
    }
}
