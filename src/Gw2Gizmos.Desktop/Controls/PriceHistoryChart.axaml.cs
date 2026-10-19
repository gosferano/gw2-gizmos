using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gw2Gizmos.Desktop.Core;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Draws the shared <see cref="PriceHistoryViewModel"/> with ScottPlot: a detail chart (buy/sell lines on the left
/// axis, volume bars on the right) clipped to the view-model's window, and a navigator over the whole history with that
/// window as a shaded span. Navigator presses and drags go to the view-model's drag logic (as in the WPF app), and
/// hovering the detail chart shows the nearest point's figures in a small readout. ScottPlot's own mouse pan/zoom is
/// off on both charts.
/// </summary>
public partial class PriceHistoryChart : UserControl
{
    private static readonly ScottPlot.Color BuyColor = ToColor(PriceHistoryColors.Buy);
    private static readonly ScottPlot.Color SellColor = ToColor(PriceHistoryColors.Sell);
    private static readonly ScottPlot.Color VolumeColor = ToColor(PriceHistoryColors.Volume);
    private static readonly ScottPlot.Color AxisColor = ToColor(PriceHistoryColors.Axis);
    private static readonly ScottPlot.Color ThumbColor = ToColor(PriceHistoryColors.Thumb);

    private PriceHistoryViewModel? _history;
    private HorizontalSpan? _thumb;
    private VerticalLine? _hoverLine;
    private double[] _times = Array.Empty<double>();

    public PriceHistoryChart()
    {
        InitializeComponent();
        DetailPlot.UserInputProcessor.Disable();
        NavigatorPlot.UserInputProcessor.Disable();

        DetailPlot.PointerMoved += OnDetailPointerMoved;
        DetailPlot.PointerExited += (_, _) => HideReadout();
        NavigatorPlot.PointerPressed += OnNavigatorPointerPressed;
        NavigatorPlot.PointerMoved += OnNavigatorPointerMoved;
        NavigatorPlot.PointerReleased += OnNavigatorPointerReleased;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_history is not null)
        {
            _history.PointsChanged -= OnPointsChanged;
            _history.WindowChanged -= OnWindowChanged;
        }

        _history = DataContext as PriceHistoryViewModel;
        if (_history is not null)
        {
            _history.PointsChanged += OnPointsChanged;
            _history.WindowChanged += OnWindowChanged;
        }

        Rebuild();
    }

    private void OnPointsChanged(object? sender, EventArgs e) => Rebuild();

    private void OnWindowChanged(object? sender, EventArgs e) => ApplyWindow();

    /// <summary>Rebuilds both charts from the view-model's points.</summary>
    private void Rebuild()
    {
        HideReadout();
        Plot detail = DetailPlot.Plot;
        Plot navigator = NavigatorPlot.Plot;
        detail.Clear();
        navigator.Clear();
        _thumb = null;
        _hoverLine = null;
        _times = Array.Empty<double>();

        StyleDetail(detail);
        StyleNavigator(navigator);

        if (_history is not { HasData: true })
        {
            DetailPlot.Refresh();
            NavigatorPlot.Refresh();
            return;
        }

        IReadOnlyList<HistoryPoint> points = _history.Points;
        _times = points.Select(p => p.Time.ToOADate()).ToArray();

        // Volume bars on the right axis, each a little narrower than the gap to its nearest neighbour: the history is
        // sparse further back and dense lately, so one width for all would overlap where polls are close.
        BarPlot volume = detail.Add.Bars(_times, points.Select(p => (double)p.Volume));
        volume.Axes.YAxis = detail.Axes.Right;
        for (int i = 0; i < volume.Bars.Count; i++)
        {
            Bar bar = volume.Bars[i];
            bar.Size = NearestGap(_times, i) * 0.8;
            bar.FillColor = VolumeColor;
            bar.LineWidth = 0;
        }

        // Buy/sell lines on the left axis; an empty side of the book is a gap, drawn as separate segments.
        AddLine(detail, points, p => p.Buy, BuyColor, width: 2, markerSize: 5);
        AddLine(detail, points, p => p.Sell, SellColor, width: 2, markerSize: 5);

        // The navigator shows the whole history; the thumb marks the detail chart's window.
        AddLine(navigator, points, p => p.Buy, AxisColor, width: 1, markerSize: 0);
        AddLine(navigator, points, p => p.Sell, AxisColor, width: 1, markerSize: 0);
        navigator.Axes.AutoScale();
        navigator.Axes.SetLimitsX(_times[0], _times[^1]);
        _thumb = navigator.Add.HorizontalSpan(_times[0], _times[^1], ThumbColor);
        _thumb.LineWidth = 0;

        _hoverLine = detail.Add.VerticalLine(_times[0], 1, AxisColor);
        _hoverLine.IsVisible = false;

        ApplyWindow();
    }

    /// <summary>Clips the detail chart to the view-model's window (rescaling both value axes to the visible points)
    /// and moves the navigator thumb to match.</summary>
    private void ApplyWindow()
    {
        if (_history is not { HasData: true } || _thumb is null)
        {
            return;
        }

        double from = _history.WindowStart.ToOADate();
        double to = _history.WindowEnd.ToOADate();
        _thumb.X1 = from;
        _thumb.X2 = to;

        Plot detail = DetailPlot.Plot;
        detail.Axes.SetLimitsX(from, to);
        FitValueAxes(detail, from, to);

        DetailPlot.Refresh();
        NavigatorPlot.Refresh();
    }

    /// <summary>Scales the price (left) and volume (right) axes to the points inside the window.</summary>
    private void FitValueAxes(Plot detail, double from, double to)
    {
        IReadOnlyList<HistoryPoint> points = _history!.Points;
        double priceMin = double.MaxValue, priceMax = double.MinValue, volumeMax = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (_times[i] < from || _times[i] > to)
            {
                continue;
            }

            foreach (int? price in new[] { points[i].Buy, points[i].Sell })
            {
                if (price is { } value)
                {
                    priceMin = Math.Min(priceMin, value);
                    priceMax = Math.Max(priceMax, value);
                }
            }

            volumeMax = Math.Max(volumeMax, points[i].Volume);
        }

        if (priceMin <= priceMax)
        {
            double pad = Math.Max((priceMax - priceMin) * 0.08, 1);
            detail.Axes.SetLimitsY(priceMin - pad, priceMax + pad, detail.Axes.Left);
        }

        detail.Axes.SetLimitsY(0, Math.Max(volumeMax * 1.1, 1), detail.Axes.Right);
    }

    // ── Navigator dragging (the view-model owns the rules: edge resize, middle pan, click-to-centre) ──

    private void OnNavigatorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_history is not { HasData: true })
        {
            return;
        }

        e.Pointer.Capture(NavigatorPlot);
        _history.BeginDrag(ToTime(NavigatorPlot, e.GetPosition(NavigatorPlot)));
    }

    private void OnNavigatorPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_history is not null && e.GetCurrentPoint(NavigatorPlot).Properties.IsLeftButtonPressed)
        {
            _history.DragTo(ToTime(NavigatorPlot, e.GetPosition(NavigatorPlot)));
        }
    }

    private void OnNavigatorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        _history?.EndDrag();
    }

    // ── Hover readout on the detail chart ──

    private void OnDetailPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_history is not { HasData: true } || _hoverLine is null)
        {
            return;
        }

        Point position = e.GetPosition(DetailPlot);
        double x = ToTime(DetailPlot, position).ToOADate();
        int index = NearestIndex(x);
        if (index < 0 || _times[index] < _history.WindowStart.ToOADate() || _times[index] > _history.WindowEnd.ToOADate())
        {
            HideReadout();
            return;
        }

        HistoryPoint point = _history.Points[index];
        ReadoutTime.Text = $"{point.Time:yyyy-MM-dd HH:mm}";
        ShowPrice(ReadoutBuy, ReadoutBuyNone, point.Buy);
        ShowPrice(ReadoutSell, ReadoutSellNone, point.Sell);
        ReadoutVolume.Text = $"Volume {point.Volume:N0}";
        Readout.IsVisible = true;
        Readout.Measure(Size.Infinity);
        double left = Math.Clamp(position.X - (Readout.DesiredSize.Width / 2), 0,
            Math.Max(0, Bounds.Width - Readout.DesiredSize.Width));
        Canvas.SetLeft(Readout, left);

        _hoverLine.X = _times[index];
        _hoverLine.IsVisible = true;
        DetailPlot.Refresh();
    }

    private void HideReadout()
    {
        Readout.IsVisible = false;
        if (_hoverLine is { IsVisible: true })
        {
            _hoverLine.IsVisible = false;
            DetailPlot.Refresh();
        }
    }

    /// <summary>Index of the recorded point nearest <paramref name="x"/> (an OA date), or -1 when there are none.</summary>
    private int NearestIndex(double x)
    {
        if (_times.Length == 0)
        {
            return -1;
        }

        int i = Array.BinarySearch(_times, x);
        if (i >= 0)
        {
            return i;
        }

        i = ~i;
        if (i == 0)
        {
            return 0;
        }

        if (i >= _times.Length)
        {
            return _times.Length - 1;
        }

        return x - _times[i - 1] <= _times[i] - x ? i - 1 : i;
    }

    // ── Helpers ──

    /// <summary>The time under an Avalonia position on <paramref name="plot"/> (ScottPlot works in scaled pixels).</summary>
    private static DateTime ToTime(AvaPlot plot, Point position)
    {
        float scale = plot.DisplayScale;
        Coordinates coordinates = plot.Plot.GetCoordinates(
            new Pixel((float)(position.X * scale), (float)(position.Y * scale)),
            plot.Plot.Axes.Bottom, plot.Plot.Axes.Left);
        double oaDate = Math.Clamp(coordinates.X, DateTime.MinValue.ToOADate() + 1, DateTime.MaxValue.ToOADate() - 1);
        return DateTime.FromOADate(oaDate);
    }

    /// <summary>Adds one series as line segments, breaking wherever <paramref name="value"/> is null.</summary>
    private static void AddLine(
        Plot plot, IReadOnlyList<HistoryPoint> points, Func<HistoryPoint, int?> value, ScottPlot.Color color,
        float width, float markerSize)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        void Flush()
        {
            if (xs.Count > 0)
            {
                Scatter segment = plot.Add.Scatter(xs.ToArray(), ys.ToArray(), color);
                segment.LineWidth = width;
                segment.MarkerSize = markerSize;
                xs.Clear();
                ys.Clear();
            }
        }

        foreach (HistoryPoint point in points)
        {
            if (value(point) is { } y)
            {
                xs.Add(point.Time.ToOADate());
                ys.Add(y);
            }
            else
            {
                Flush();
            }
        }

        Flush();
    }

    private static void StyleDetail(Plot plot)
    {
        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = Colors.Transparent;
        plot.HideLegend();

        // Swap in the date axis before colouring: it replaces the bottom axis, which would otherwise keep ScottPlot's
        // default (black) labels.
        var bottom = plot.Axes.DateTimeTicksBottom();
        if (bottom.TickGenerator is DateTimeAutomatic dates)
        {
            dates.LabelFormatter = PriceHistoryViewModel.FormatTime;
        }

        plot.Axes.Color(AxisColor);
        plot.Grid.MajorLineColor = AxisColor.WithAlpha(0.15);

        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = PriceHistoryViewModel.FormatGold };
        foreach (IAxis axis in new IAxis[] { plot.Axes.Bottom, plot.Axes.Left, plot.Axes.Right })
        {
            axis.TickLabelStyle.FontSize = 11;
        }
    }

    private static void StyleNavigator(Plot plot)
    {
        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = Colors.Transparent;
        plot.HideAxesAndGrid();
        plot.HideLegend();
        plot.Axes.Margins(0, 0.1);
    }

    /// <summary>The gap (OA days) from point <paramref name="i"/> to the closer of its neighbours, for its bar's
    /// width.</summary>
    private static double NearestGap(double[] times, int i)
    {
        if (times.Length < 2)
        {
            return 1.0 / 24;
        }

        double before = i > 0 ? times[i] - times[i - 1] : double.MaxValue;
        double after = i < times.Length - 1 ? times[i + 1] - times[i] : double.MaxValue;
        return Math.Max(Math.Min(before, after), 1.0 / (24 * 60));
    }

    /// <summary>Shows a price with coin icons, or the dash when that side of the book was empty.</summary>
    private static void ShowPrice(CoinDisplay coins, TextBlock none, int? copper)
    {
        coins.Copper = copper;
        coins.IsVisible = copper is not null;
        none.IsVisible = copper is null;
    }

    private static ScottPlot.Color ToColor(uint argb) =>
        new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));
}
