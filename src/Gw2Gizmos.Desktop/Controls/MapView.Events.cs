using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Scheduled events pinned at their waypoints as small badges with the most pressing event's name and timer, plus how
/// many more start there. From the zoom waypoints show at, a badge stands above its waypoint on a short stem, so the
/// waypoint stays visible and clickable. Badges give way to more pressing ones they'd overlap: active first, then the
/// soonest. The timers tick once a second while there are badges.
/// </summary>
public sealed partial class MapView
{
    public static readonly StyledProperty<IReadOnlyList<MapEventSpot>?> EventSpotsProperty = AvaloniaProperty.Register<
        MapView,
        IReadOnlyList<MapEventSpot>?
    >(nameof(EventSpots));

    private const double EventSpotsFromZoom = 2;

    /// <summary>How far above its waypoint a badge stands once waypoints are drawn.</summary>
    private const double EventBadgeLift = 26;

    /// <summary>An event starting within this is highlighted as soon.</summary>
    private static readonly TimeSpan SoonWindow = TimeSpan.FromMinutes(15);

    private static readonly IBrush BadgeBackground = new SolidColorBrush(Color.FromArgb(0xE6, 0x1C, 0x1C, 0x1C));
    private static readonly IPen ActiveBorder = new Pen(new SolidColorBrush(Color.FromRgb(0x5C, 0xD6, 0x6B)), 1.5);
    private static readonly IPen SoonBorder = new Pen(new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42)), 1.5);
    private static readonly IPen LaterBorder = new Pen(new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF)), 1);
    private static readonly IBrush ActiveText = new SolidColorBrush(Color.FromRgb(0x7E, 0xE8, 0x8B));
    private static readonly IBrush SoonText = new SolidColorBrush(Color.FromRgb(0xF5, 0xC5, 0x42));
    private static readonly IBrush LaterText = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
    private static readonly IPen StemPen = new Pen(new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF)), 1.5);

    private DispatcherTimer? _eventTick;

    /// <summary>The scheduled events to pin, grouped by waypoint.</summary>
    public IReadOnlyList<MapEventSpot>? EventSpots
    {
        get => GetValue(EventSpotsProperty);
        set => SetValue(EventSpotsProperty, value);
    }

    private void DrawEventSpots(DrawingContext context, Rect bounds, double left, double top, double scale)
    {
        UpdateEventTick();
        if (EventSpots is not { Count: > 0 } spots || Zoom < EventSpotsFromZoom)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool lifted = Zoom >= WaypointsFromZoom;
        var drawn = new List<Rect>();

        // Most pressing first, so where badges would overlap the one drawn is the one that matters most.
        var ordered = spots
            .Select(spot => (Spot: spot, Events: spot.ByUrgency(now)))
            .OrderByDescending(s => s.Events[0].Timing.IsActive)
            .ThenBy(s => s.Events[0].Timing.SortKey);

        foreach ((MapEventSpot spot, IReadOnlyList<(ScheduledEvent Event, EventTiming Timing)> events) in ordered)
        {
            Point at = new((spot.X - left) * scale, (spot.Y - top) * scale);
            if (!IsNear(bounds, at, 150))
            {
                continue;
            }

            (ScheduledEvent first, EventTiming timing) = events[0];
            string more = events.Count > 1 ? $"  +{events.Count - 1}" : "";
            FormattedText name = Text(first.Name, 11, FontWeight.SemiBold, Brushes.White);
            FormattedText time = Text(
                $"  {timing.ShortCountdown}{more}",
                11,
                FontWeight.Normal,
                TimerBrush(timing, now)
            );

            const double paddingX = 6;
            const double paddingY = 3;
            double width = name.Width + time.Width + paddingX * 2;
            double height = Math.Max(name.Height, time.Height) + paddingY * 2;
            double centerY = lifted ? at.Y - EventBadgeLift : at.Y;
            Rect badge = new(Math.Round(at.X - width / 2), Math.Round(centerY - height / 2), width, height);
            if (drawn.Any(other => other.Intersects(badge)))
            {
                continue;
            }

            drawn.Add(badge);
            if (lifted)
            {
                context.DrawLine(StemPen, new Point(at.X, badge.Bottom), new Point(at.X, at.Y - 10));
            }

            context.DrawRectangle(BadgeBackground, BorderFor(timing, now), badge, 4, 4);
            context.DrawText(name, new Point(badge.X + paddingX, badge.Y + paddingY));
            context.DrawText(time, new Point(badge.X + paddingX + name.Width, badge.Y + paddingY));
            _hitTargets.Add((badge, spot));
        }
    }

    /// <summary>A tooltip line per event at the spot, most pressing first, each in its timer's colour.</summary>
    private static IReadOnlyList<FormattedText> EventLines(MapEventSpot spot)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return spot.ByUrgency(now)
            .Select(e =>
                Text($"{e.Event.Name} — {e.Timing.Countdown}", 12, FontWeight.Normal, TimerBrush(e.Timing, now))
            )
            .ToList();
    }

    private static IPen BorderFor(EventTiming timing, DateTimeOffset now) =>
        timing.IsActive ? ActiveBorder
        : timing.SortKey - now <= SoonWindow ? SoonBorder
        : LaterBorder;

    private static IBrush TimerBrush(EventTiming timing, DateTimeOffset now) =>
        timing.IsActive ? ActiveText
        : timing.SortKey - now <= SoonWindow ? SoonText
        : LaterText;

    /// <summary>
    /// Keeps the timers ticking once a second while there are badges or protected objectives, and stops when there are
    /// none.
    /// </summary>
    private void UpdateEventTick()
    {
        bool wanted = (EventSpots is { Count: > 0 } && Zoom >= EventSpotsFromZoom) || HasProtectedObjectives();
        if (wanted && _eventTick is null)
        {
            _eventTick = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Render,
                (_, _) => InvalidateVisual()
            );
            _eventTick.Start();
        }
        else if (!wanted && _eventTick is not null)
        {
            _eventTick.Stop();
            _eventTick = null;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _eventTick?.Stop();
        _eventTick = null;
    }
}
