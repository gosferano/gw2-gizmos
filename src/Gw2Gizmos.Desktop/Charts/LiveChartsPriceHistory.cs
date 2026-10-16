using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Core.Mvvm;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Gw2Gizmos.Desktop.Charts;

/// <summary>
/// Draws the shared <see cref="PriceHistoryViewModel"/> with LiveCharts for the WPF Items page: a detailed chart
/// (buy/sell lines plus a volume column series) whose X range follows the view-model's window, and a short navigator
/// over the whole history with that window as a draggable section. Pointer positions are converted to time and handed
/// to the view-model's drag logic.
/// </summary>
public sealed class LiveChartsPriceHistory : ViewModelBase
{
    private static readonly SKColor BuyColor = new(PriceHistoryColors.Buy);
    private static readonly SKColor SellColor = new(PriceHistoryColors.Sell);
    private static readonly SKColor VolumeColor = new(PriceHistoryColors.Volume);
    private static readonly SKColor AxisColor = new(PriceHistoryColors.Axis);
    private static readonly SKColor ThumbColor = new(PriceHistoryColors.Thumb);

    private readonly PriceHistoryViewModel _history;

    public LiveChartsPriceHistory(PriceHistoryViewModel history)
    {
        _history = history;
        var axisPaint = new SolidColorPaint(AxisColor);

        PriceAxis = new Axis { Labeler = GoldLabeler, TextSize = 11, NamePaint = axisPaint, LabelsPaint = axisPaint };
        VolumeAxis = new Axis { Position = AxisPosition.End, TextSize = 11, ShowSeparatorLines = false, LabelsPaint = axisPaint };
        TimeAxis = new Axis { Labeler = TimeLabeler, TextSize = 11, LabelsPaint = axisPaint };
        NavigatorTimeAxis = new Axis { IsVisible = false };
        NavigatorValueAxis = new Axis { IsVisible = false };

        Thumb = new RectangularSection { Fill = new SolidColorPaint(ThumbColor) };

        PointerDownCommand = new RelayCommand<PointerCommandArgs>(args =>
        {
            if (args is not null)
            {
                _history.BeginDrag(ToTime(args));
            }
        });
        PointerMoveCommand = new RelayCommand<PointerCommandArgs>(args =>
        {
            if (args is not null)
            {
                _history.DragTo(ToTime(args));
            }
        });
        PointerUpCommand = new RelayCommand<PointerCommandArgs>(_ => _history.EndDrag());

        _history.PointsChanged += (_, _) => Rebuild();
        _history.WindowChanged += (_, _) => ApplyWindow();
        _history.PropertyChanged += OnHistoryPropertyChanged;
        Rebuild();
        ApplyWindow();
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

    public bool HasData => _history.HasData;

    private Axis PriceAxis { get; }
    private Axis VolumeAxis { get; }
    private Axis TimeAxis { get; }
    private Axis NavigatorTimeAxis { get; }
    private Axis NavigatorValueAxis { get; }
    private RectangularSection Thumb { get; }

    private void OnHistoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PriceHistoryViewModel.HasData))
        {
            OnPropertyChanged(nameof(HasData));
        }
    }

    /// <summary>Rebuilds both charts' series from the view-model's points.</summary>
    private void Rebuild()
    {
        if (!_history.HasData)
        {
            Series = Array.Empty<ISeries>();
            NavigatorSeries = Array.Empty<ISeries>();
            RaiseChartChanged();
            return;
        }

        var buy = _history.Points.Select(p => new DateTimePoint(p.Time, p.Buy)).ToArray();
        var sell = _history.Points.Select(p => new DateTimePoint(p.Time, p.Sell)).ToArray();
        var volume = _history.Points.Select(p => new DateTimePoint(p.Time, p.Volume)).ToArray();

        Series = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Name = "Volume",
                Values = volume,
                ScalesYAt = 1,
                Fill = new SolidColorPaint(VolumeColor),
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
                Values = buy,
                Stroke = new SolidColorPaint(AxisColor, 1),
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            },
            new LineSeries<DateTimePoint>
            {
                Values = sell,
                Stroke = new SolidColorPaint(AxisColor, 1),
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            }
        };

        RaiseChartChanged();
    }

    /// <summary>Clips the detail chart to the view-model's window and moves the navigator thumb to match.</summary>
    private void ApplyWindow()
    {
        if (!_history.HasData)
        {
            return;
        }

        double from = _history.WindowStart.Ticks;
        double to = _history.WindowEnd.Ticks;
        Thumb.Xi = from;
        Thumb.Xj = to;
        TimeAxis.MinLimit = from;
        TimeAxis.MaxLimit = to;
    }

    private static DateTime ToTime(PointerCommandArgs args)
    {
        var chart = (ICartesianChartView)args.Chart;
        double ticks = chart.ScalePixelsToData(args.PointerPosition).X;
        return new DateTime((long)Math.Clamp(ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks));
    }

    private void RaiseChartChanged()
    {
        OnPropertyChanged(nameof(Series));
        OnPropertyChanged(nameof(NavigatorSeries));
    }

    private static string TimeLabeler(double ticks) =>
        ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks
            ? string.Empty
            : PriceHistoryViewModel.FormatTime(new DateTime((long)ticks));

    private static string GoldLabeler(double copper) => PriceHistoryViewModel.FormatGold(copper);
}
