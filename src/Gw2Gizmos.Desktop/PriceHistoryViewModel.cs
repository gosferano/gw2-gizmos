using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Desktop.Mvvm;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows.Input;

namespace Gw2Gizmos.Desktop;

/// <summary>One recorded point of an item's trading-post history, flattened for charting.</summary>
public readonly record struct HistoryPoint(DateTime Time, int Buy, int Sell, int Volume);

/// <summary>
/// Backs the price-history view for the selected market item: a detailed top chart (buy/sell lines plus a
/// volume column series) whose visible time range is driven by a draggable window on a short navigator
/// chart below it. Dragging on the navigator pans/zooms the top chart by setting its X-axis limits.
/// </summary>
public sealed class PriceHistoryViewModel : ViewModelBase
{
    private static readonly SKColor BuyColor = new(0x4C, 0xAF, 0x50);
    private static readonly SKColor SellColor = new(0xE5, 0x73, 0x73);
    private static readonly SKColor VolumeColor = new(0x60, 0x7D, 0x8B);
    private static readonly SKColor AxisColor = new(0x9E, 0x9E, 0x9E);
    private static readonly SKColor ThumbColor = new(0x55, 0x90, 0xCA, 0x60);

    private static readonly double MinWindowTicks = TimeSpan.FromMinutes(15).Ticks;

    private bool _hasData;
    private Drag _drag = Drag.None;
    private double _dragOffset;
    private double _dataMin;
    private double _dataMax;

    /// <summary>What a navigator drag is doing: moving the whole window, or stretching one of its edges.</summary>
    private enum Drag
    {
        None,
        Pan,
        ResizeStart,
        ResizeEnd
    }

    public PriceHistoryViewModel()
    {
        var axisPaint = new SolidColorPaint(AxisColor);

        PriceAxis = new Axis { Labeler = GoldLabeler, TextSize = 11, NamePaint = axisPaint, LabelsPaint = axisPaint };
        VolumeAxis = new Axis { Position = AxisPosition.End, TextSize = 11, ShowSeparatorLines = false, LabelsPaint = axisPaint };
        TimeAxis = new Axis { Labeler = TimeLabeler, TextSize = 11, LabelsPaint = axisPaint };
        NavigatorTimeAxis = new Axis { IsVisible = false };
        NavigatorValueAxis = new Axis { IsVisible = false };

        Thumb = new RectangularSection { Fill = new SolidColorPaint(ThumbColor) };

        PointerDownCommand = new RelayCommand<PointerCommandArgs>(OnPointerDown);
        PointerMoveCommand = new RelayCommand<PointerCommandArgs>(OnPointerMove);
        PointerUpCommand = new RelayCommand<PointerCommandArgs>(_ => _drag = Drag.None);
    }

    public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();
    public ISeries[] NavigatorSeries { get; private set; } = Array.Empty<ISeries>();

    public Axis[] TimeAxes => new[] { TimeAxis };
    public Axis[] ValueAxes => new[] { PriceAxis, VolumeAxis };
    public Axis[] NavigatorTimeAxes => new[] { NavigatorTimeAxis };
    public Axis[] NavigatorValueAxes => new[] { NavigatorValueAxis };
    public RectangularSection[] Sections => new[] { Thumb };

    public ICommand PointerDownCommand { get; }
    public ICommand PointerMoveCommand { get; }
    public ICommand PointerUpCommand { get; }

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    private Axis PriceAxis { get; }
    private Axis VolumeAxis { get; }
    private Axis TimeAxis { get; }
    private Axis NavigatorTimeAxis { get; }
    private Axis NavigatorValueAxis { get; }
    private RectangularSection Thumb { get; }

    /// <summary>Rebuilds both charts from an item's full history; shows the most recent quarter by default.</summary>
    public void Load(IReadOnlyList<HistoryPoint> history)
    {
        if (history.Count < 2)
        {
            Series = Array.Empty<ISeries>();
            NavigatorSeries = Array.Empty<ISeries>();
            HasData = false;
            RaiseChartChanged();
            return;
        }

        var buy = history.Select(p => new DateTimePoint(p.Time, p.Buy)).ToArray();
        var sell = history.Select(p => new DateTimePoint(p.Time, p.Sell)).ToArray();
        var volume = history.Select(p => new DateTimePoint(p.Time, p.Volume)).ToArray();

        Series = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Name = "Volume",
                Values = volume,
                ScalesYAt = 1,
                Fill = new SolidColorPaint(VolumeColor.WithAlpha(0x80)),
                Padding = 0
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Buy",
                Values = buy,
                Stroke = new SolidColorPaint(BuyColor, 2),
                Fill = null,
                // A small marker gives each point a hover area, so the tooltip triggers anywhere along the
                // line rather than only over the thin volume bars.
                GeometrySize = 7,
                GeometryFill = new SolidColorPaint(BuyColor),
                GeometryStroke = null,
                LineSmoothness = 0
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Sell",
                Values = sell,
                Stroke = new SolidColorPaint(SellColor, 2),
                Fill = null,
                GeometrySize = 7,
                GeometryFill = new SolidColorPaint(SellColor),
                GeometryStroke = null,
                LineSmoothness = 0
            }
        };

        NavigatorSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = sell,
                Stroke = new SolidColorPaint(AxisColor, 1),
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            }
        };

        _dataMin = history[0].Time.Ticks;
        _dataMax = history[^1].Time.Ticks;

        // Default window: the most recent ~25% of the recorded span.
        double window = Math.Max((_dataMax - _dataMin) * 0.25, MinWindowTicks);
        SetWindow(_dataMax - window, _dataMax);

        HasData = true;
        RaiseChartChanged();
    }

    private void SetWindow(double from, double to)
    {
        from = Math.Max(from, _dataMin);
        to = Math.Min(to, _dataMax);
        Thumb.Xi = from;
        Thumb.Xj = to;
        TimeAxis.MinLimit = from;
        TimeAxis.MaxLimit = to;
    }

    private void OnPointerDown(PointerCommandArgs? args)
    {
        if (args is null || !HasData)
        {
            return;
        }

        double x = ToDataX(args);
        double from = Thumb.Xi!.Value;
        double to = Thumb.Xj!.Value;
        // An edge "grab zone" ~8% of the window wide, so the left/right edges resize and the middle pans.
        double edge = Math.Max((to - from) * 0.08, TimeSpan.FromMinutes(5).Ticks);

        if (x >= from - edge && x <= to + edge)
        {
            if (Math.Abs(x - from) <= edge)
            {
                _drag = Drag.ResizeStart;
            }
            else if (Math.Abs(x - to) <= edge)
            {
                _drag = Drag.ResizeEnd;
            }
            else
            {
                _drag = Drag.Pan;
                _dragOffset = x - (from + to) / 2;
            }
        }
        else
        {
            // Clicked away from the window — jump it to centre on the click, then pan from there.
            _drag = Drag.Pan;
            _dragOffset = 0;
            CenterWindowOn(x);
        }
    }

    private void OnPointerMove(PointerCommandArgs? args)
    {
        if (_drag == Drag.None || args is null)
        {
            return;
        }

        double x = ToDataX(args);
        switch (_drag)
        {
            case Drag.ResizeStart:
                SetWindow(Math.Min(x, Thumb.Xj!.Value - MinWindowTicks), Thumb.Xj!.Value);
                break;
            case Drag.ResizeEnd:
                SetWindow(Thumb.Xi!.Value, Math.Max(x, Thumb.Xi!.Value + MinWindowTicks));
                break;
            case Drag.Pan:
                CenterWindowOn(x - _dragOffset);
                break;
        }
    }

    private void CenterWindowOn(double center)
    {
        double half = (Thumb.Xj!.Value - Thumb.Xi!.Value) / 2;
        double from = center - half;
        double to = center + half;

        // Keep the window width when panning into a boundary instead of letting it shrink.
        if (from < _dataMin)
        {
            (from, to) = (_dataMin, _dataMin + 2 * half);
        }
        else if (to > _dataMax)
        {
            (from, to) = (_dataMax - 2 * half, _dataMax);
        }

        SetWindow(from, to);
    }

    private static double ToDataX(PointerCommandArgs args)
    {
        var chart = (ICartesianChartView)args.Chart;
        return chart.ScalePixelsToData(args.PointerPosition).X;
    }

    private void RaiseChartChanged()
    {
        OnPropertyChanged(nameof(Series));
        OnPropertyChanged(nameof(NavigatorSeries));
    }

    private static string TimeLabeler(double ticks)
    {
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            return string.Empty;
        }

        var time = new DateTime((long)ticks);
        return time.ToString(time.TimeOfDay == TimeSpan.Zero ? "MM-dd" : "MM-dd HH:mm");
    }

    private static string GoldLabeler(double copper)
    {
        if (copper <= 0)
        {
            return "0";
        }

        long total = (long)copper;
        long gold = total / 10000;
        long silver = total % 10000 / 100;
        return gold > 0 ? $"{gold}g {silver}s" : $"{silver}s";
    }
}
