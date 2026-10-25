using System;
using System.Collections.Generic;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>One recorded point of an item's trading-post history, flattened for charting. Time is UTC, so it keeps
/// moving forward through daylight saving changes. Buy/Sell are null when that side of the book was empty at the
/// time, which the line charts render as a gap.</summary>
public readonly record struct HistoryPoint(DateTime Time, int? Buy, int? Sell, int Volume);

/// <summary>
/// The selected item's price history, independent of any chart library: the points, and the time window the detailed
/// chart shows. Each UI head draws two charts from it — a detailed one (buy/sell lines over volume bars) clipped to
/// <see cref="WindowStart"/>..<see cref="WindowEnd"/>, and a short navigator over the whole history with that window
/// drawn as a draggable thumb. Dragging on the navigator goes through <see cref="BeginDrag"/> / <see cref="DragTo"/> /
/// <see cref="EndDrag"/> in data coordinates (UTC time), so both heads behave the same.
/// </summary>
public sealed class PriceHistoryViewModel : ViewModelBase
{
    /// <summary>The narrowest window the navigator can be shrunk to.</summary>
    private static readonly TimeSpan MinWindow = TimeSpan.FromMinutes(15);

    private IReadOnlyList<HistoryPoint> _points = Array.Empty<HistoryPoint>();
    private DateTime _windowStart;
    private DateTime _windowEnd;
    private Drag _drag = Drag.None;
    private TimeSpan _dragOffset;

    /// <summary>What a navigator drag is doing: moving the whole window, or stretching one of its edges.</summary>
    private enum Drag
    {
        None,
        Pan,
        ResizeStart,
        ResizeEnd
    }

    /// <summary>Raised when the history is replaced (a new item selected); heads rebuild their series.</summary>
    public event EventHandler? PointsChanged;

    /// <summary>Raised when the visible window moves; heads update the detail chart's range and the thumb.</summary>
    public event EventHandler? WindowChanged;

    /// <summary>The full history, oldest first; empty when there's nothing to chart.</summary>
    public IReadOnlyList<HistoryPoint> Points => _points;

    /// <summary>True when there are at least two points to draw.</summary>
    public bool HasData => _points.Count >= 2;

    /// <summary>The first recorded time.</summary>
    public DateTime DataStart => HasData ? _points[0].Time : default;

    /// <summary>The last recorded time.</summary>
    public DateTime DataEnd => HasData ? _points[^1].Time : default;

    /// <summary>Left edge of the detailed chart's visible range.</summary>
    public DateTime WindowStart => _windowStart;

    /// <summary>Right edge of the detailed chart's visible range.</summary>
    public DateTime WindowEnd => _windowEnd;

    /// <summary>Replaces the history; shows the most recent quarter of it by default.</summary>
    public void Load(IReadOnlyList<HistoryPoint> history)
    {
        _points = history.Count >= 2 ? history : Array.Empty<HistoryPoint>();
        _drag = Drag.None;
        OnPropertyChanged(nameof(Points));
        OnPropertyChanged(nameof(HasData));
        PointsChanged?.Invoke(this, EventArgs.Empty);

        if (HasData)
        {
            // Default window: the most recent ~25% of the recorded span.
            TimeSpan window = Max(TimeSpan.FromTicks((DataEnd - DataStart).Ticks / 4), MinWindow);
            SetWindow(DataEnd - window, DataEnd);
        }
    }

    /// <summary>
    /// Starts a navigator drag at <paramref name="time"/>: near an edge of the window it resizes that edge, inside it
    /// pans, and outside it the window jumps to centre on the click and then pans.
    /// </summary>
    public void BeginDrag(DateTime time)
    {
        if (!HasData)
        {
            return;
        }

        // An edge "grab zone" ~8% of the window wide, so the left/right edges resize and the middle pans.
        TimeSpan edge = Max(TimeSpan.FromTicks((_windowEnd - _windowStart).Ticks * 8 / 100), TimeSpan.FromMinutes(5));

        if (time >= _windowStart - edge && time <= _windowEnd + edge)
        {
            if ((time - _windowStart).Duration() <= edge)
            {
                _drag = Drag.ResizeStart;
            }
            else if ((time - _windowEnd).Duration() <= edge)
            {
                _drag = Drag.ResizeEnd;
            }
            else
            {
                _drag = Drag.Pan;
                _dragOffset = time - Center;
            }
        }
        else
        {
            // Clicked away from the window — jump it to centre on the click, then pan from there.
            _drag = Drag.Pan;
            _dragOffset = TimeSpan.Zero;
            CenterWindowOn(time);
        }
    }

    /// <summary>Continues a navigator drag begun with <see cref="BeginDrag"/>.</summary>
    public void DragTo(DateTime time)
    {
        switch (_drag)
        {
            case Drag.ResizeStart:
                SetWindow(Min(time, _windowEnd - MinWindow), _windowEnd);
                break;
            case Drag.ResizeEnd:
                SetWindow(_windowStart, Max(time, _windowStart + MinWindow));
                break;
            case Drag.Pan:
                CenterWindowOn(time - _dragOffset);
                break;
        }
    }

    /// <summary>Ends a navigator drag.</summary>
    public void EndDrag() => _drag = Drag.None;

    /// <summary>Axis label for a local time: just the date at midnight, otherwise date and time.</summary>
    public static string FormatTime(DateTime time) =>
        time.ToString(time.TimeOfDay == TimeSpan.Zero ? "MM-dd" : "MM-dd HH:mm");

    /// <summary>Axis label for a copper price, as g/s/c.</summary>
    public static string FormatGold(double copper) => Coin.Format((long)Math.Round(copper));

    private DateTime Center => _windowStart + TimeSpan.FromTicks((_windowEnd - _windowStart).Ticks / 2);

    private void CenterWindowOn(DateTime center)
    {
        TimeSpan width = _windowEnd - _windowStart;
        DateTime from = center - TimeSpan.FromTicks(width.Ticks / 2);
        DateTime to = from + width;

        // Keep the window width when panning into a boundary instead of letting it shrink.
        if (from < DataStart)
        {
            (from, to) = (DataStart, DataStart + width);
        }
        else if (to > DataEnd)
        {
            (from, to) = (DataEnd - width, DataEnd);
        }

        SetWindow(from, to);
    }

    private void SetWindow(DateTime from, DateTime to)
    {
        _windowStart = Max(from, DataStart);
        _windowEnd = Min(to, DataEnd);
        OnPropertyChanged(nameof(WindowStart));
        OnPropertyChanged(nameof(WindowEnd));
        WindowChanged?.Invoke(this, EventArgs.Empty);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}

/// <summary>The price charts' colours, shared by every UI head (ARGB).</summary>
public static class PriceHistoryColors
{
    public const uint Buy = 0xFF4CAF50;
    public const uint Sell = 0xFFE57373;
    public const uint Volume = 0x80607D8B;
    public const uint Axis = 0xFF9E9E9E;
    public const uint Thumb = 0x605590CA;
}
