using Avalonia;
using Avalonia.Controls;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Gliding to a spot (<see cref="Flight"/>) instead of jumping there, and keeping a followed <see cref="Player"/> in
/// the middle. A long glide zooms out a little on the way and back in, so it's clear where the map went. Dragging,
/// scrolling or double-clicking stops a glide.
/// </summary>
public sealed partial class MapView
{
    public static readonly StyledProperty<MapFlight?> FlightProperty = AvaloniaProperty.Register<MapView, MapFlight?>(
        nameof(Flight)
    );

    private static readonly TimeSpan BaseFlightDuration = TimeSpan.FromMilliseconds(450);

    /// <summary>How much longer a glide takes per zoom level it dips out.</summary>
    private static readonly TimeSpan FlightDurationPerDip = TimeSpan.FromMilliseconds(150);

    /// <summary>The most zoom levels a glide dips out on the way.</summary>
    private const double MaxFlightDip = 3;

    private FlightPath? _flightPath;

    /// <summary>Each new value glides the view to its spot and zoom.</summary>
    public MapFlight? Flight
    {
        get => GetValue(FlightProperty);
        set => SetValue(FlightProperty, value);
    }

    private void OnFlightPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == FlightProperty && Flight is { } flight)
        {
            StartFlight(flight.X, flight.Y, flight.Zoom);
        }
        else if (change.Property == PlayerProperty && IsFollowing && Player is { } player)
        {
            // Mid-glide, aim at where the character is now; otherwise keep them in the middle.
            if (_flightPath is { } path)
            {
                _flightPath = path with { ToX = player.X, ToY = player.Y };
            }
            else
            {
                MoveCenterTo(player.X, player.Y);
            }
        }
    }

    private void StartFlight(double x, double y, double zoom)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);

        // How far the glide goes on screen, at the farther-out of its two zooms: a jump beyond the view dips out.
        double screenDistance =
            Math.Sqrt(Math.Pow(x - CenterX, 2) + Math.Pow(y - CenterY, 2))
            * Math.Pow(2, Math.Min(Zoom, zoom) - MapContinent.NativeZoom);
        double viewSize = Math.Max(1, Math.Max(Bounds.Width, Bounds.Height));
        double dip = Math.Clamp(Math.Log2(screenDistance / viewSize), 0, MaxFlightDip);
        dip = Math.Min(dip, Math.Min(Zoom, zoom) - MinZoom);

        TimeSpan duration = BaseFlightDuration + FlightDurationPerDip * dip;
        bool alreadyFlying = _flightPath is not null;
        _flightPath = new FlightPath(CenterX, CenterY, Zoom, x, y, zoom, dip, DateTime.UtcNow, duration);
        if (!alreadyFlying)
        {
            RequestFlightFrame();
        }
    }

    private void RequestFlightFrame()
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.RequestAnimationFrame(_ => FlightFrame());
        }
        else
        {
            // Not shown: land straight away.
            Land();
        }
    }

    private void FlightFrame()
    {
        if (_flightPath is not { } path)
        {
            return;
        }

        double progress = Math.Clamp((DateTime.UtcNow - path.Start) / path.Duration, 0, 1);
        if (progress >= 1)
        {
            Land();
            return;
        }

        double eased = EaseInOut(progress);
        Zoom = Lerp(path.FromZoom, path.ToZoom, eased) - path.Dip * Math.Sin(Math.PI * eased);
        MoveCenterTo(Lerp(path.FromX, path.ToX, eased), Lerp(path.FromY, path.ToY, eased));
        RequestFlightFrame();
    }

    private void Land()
    {
        if (_flightPath is not { } path)
        {
            return;
        }

        _flightPath = null;
        Zoom = path.ToZoom;
        MoveCenterTo(path.ToX, path.ToY);
    }

    /// <summary>Stops a glide where it is, for the user taking over the map.</summary>
    private void CancelFlight() => _flightPath = null;

    private static double Lerp(double from, double to, double progress) => from + (to - from) * progress;

    private static double EaseInOut(double t) => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    /// <param name="Dip">How many zoom levels the glide dips out at its middle.</param>
    private sealed record FlightPath(
        double FromX,
        double FromY,
        double FromZoom,
        double ToX,
        double ToY,
        double ToZoom,
        double Dip,
        DateTime Start,
        TimeSpan Duration
    );
}
