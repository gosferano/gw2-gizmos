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
        // Not begun yet: it begins at its first frame, from wherever the view is then, once the map is on screen.
        bool alreadyFlying = _flightPath is not null;
        _flightPath = new FlightPath(
            CenterX,
            CenterY,
            Zoom,
            x,
            y,
            Math.Clamp(zoom, LowestZoom, MaxZoom),
            0,
            null,
            default
        );
        if (!alreadyFlying)
        {
            RequestFlightFrame();
        }
    }

    /// <summary>
    /// Asks for the next frame. A map not on screen yet (its page just opened) gets none until it's shown: its glide
    /// waits, rather than landing at once.
    /// </summary>
    private void RequestFlightFrame() => TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => FlightFrame());

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_flightPath is not null)
        {
            RequestFlightFrame();
        }
    }

    private void FlightFrame()
    {
        if (_flightPath is not { } path)
        {
            return;
        }

        if (path.Start is not { } start)
        {
            // The glide's first frame: once the map has its size, plan the glide from where the view is.
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                RequestFlightFrame();
                return;
            }

            _flightPath = Begin(path);
            RequestFlightFrame();
            return;
        }

        double progress = Math.Clamp((DateTime.UtcNow - start) / path.Duration, 0, 1);
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

    /// <summary>
    /// Starts a glide from the view as it is now. One going further than the view is wide (at the farther-out of its
    /// two zooms) dips out on the way, and takes longer for it.
    /// </summary>
    private FlightPath Begin(FlightPath path)
    {
        double screenDistance =
            Math.Sqrt(Math.Pow(path.ToX - CenterX, 2) + Math.Pow(path.ToY - CenterY, 2))
            * Math.Pow(2, Math.Min(Zoom, path.ToZoom) - MapContinent.NativeZoom);
        double viewSize = Math.Max(Bounds.Width, Bounds.Height);
        double dip = Math.Clamp(Math.Log2(screenDistance / viewSize), 0, MaxFlightDip);
        dip = Math.Max(0, Math.Min(dip, Math.Min(Zoom, path.ToZoom) - LowestZoom));
        return path with
        {
            FromX = CenterX,
            FromY = CenterY,
            FromZoom = Zoom,
            Dip = dip,
            Start = DateTime.UtcNow,
            Duration = BaseFlightDuration + FlightDurationPerDip * dip,
        };
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
    /// <param name="Start">When the glide began; null until its first frame.</param>
    private sealed record FlightPath(
        double FromX,
        double FromY,
        double FromZoom,
        double ToX,
        double ToY,
        double ToZoom,
        double Dip,
        DateTime? Start,
        TimeSpan Duration
    );
}
