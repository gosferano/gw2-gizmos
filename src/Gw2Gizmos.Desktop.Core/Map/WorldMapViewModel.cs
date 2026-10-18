using System;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the World map page: Tyria's map and where it's looking. A singleton, so the map stays where it was left when
/// coming back to the page.
/// </summary>
public sealed class WorldMapViewModel : ViewModelBase
{
    /// <summary>Lion's Arch, at a zoom that shows central Tyria around it.</summary>
    private const double HomeX = 49216;
    private const double HomeY = 31488;
    private const double HomeZoom = 3;

    private double _centerX = HomeX;
    private double _centerY = HomeY;
    private double _zoom = HomeZoom;

    public WorldMapViewModel()
    {
        ZoomInCommand = new RelayCommand(() => Zoom = Math.Min(Zoom + 1, MaxZoom));
        ZoomOutCommand = new RelayCommand(() => Zoom = Math.Max(Zoom - 1, MinZoom));
        HomeCommand = new RelayCommand(GoHome);
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

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }

    /// <summary>Back to central Tyria.</summary>
    public ICommand HomeCommand { get; }

    private void GoHome()
    {
        CenterX = HomeX;
        CenterY = HomeY;
        Zoom = HomeZoom;
    }
}
