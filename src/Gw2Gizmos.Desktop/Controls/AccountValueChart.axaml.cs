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
/// Draws an <see cref="AccountValueViewModel"/>'s points with ScottPlot: the total as a bold line over one thinner line
/// per category (coin, materials, …) in the colours the page's breakdown uses. Hovering shows the nearest point's
/// time and total. Times are plotted in UTC with local ticks (<see cref="LocalTimeTicks"/>). ScottPlot's own mouse
/// pan/zoom is off; the range comes from the page's presets.
/// </summary>
public partial class AccountValueChart : UserControl
{
    private static readonly ScottPlot.Color AxisColor = ToColor(PriceHistoryColors.Axis);

    private AccountValueViewModel? _model;
    private VerticalLine? _hoverLine;
    private double[] _times = Array.Empty<double>();

    public AccountValueChart()
    {
        InitializeComponent();
        Plot.UserInputProcessor.Disable();
        Plot.PointerMoved += OnPointerMoved;
        Plot.PointerExited += (_, _) => HideReadout();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_model is not null)
        {
            _model.PointsChanged -= OnPointsChanged;
        }

        _model = DataContext as AccountValueViewModel;
        if (_model is not null)
        {
            _model.PointsChanged += OnPointsChanged;
        }

        Rebuild();
    }

    private void OnPointsChanged(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        HideReadout();
        ScottPlot.Plot plot = Plot.Plot;
        plot.Clear();
        _hoverLine = null;
        Style(plot);

        IReadOnlyList<AccountValuePoint> points = _model?.Points ?? Array.Empty<AccountValuePoint>();
        _times = points.Select(p => p.Time.UtcDateTime.ToOADate()).ToArray();
        if (points.Count == 0)
        {
            Plot.Refresh();
            return;
        }

        foreach (AccountValueCategory category in AccountValueCategories.All)
        {
            double[] values = points.Select(p => (double)category.Value(p)).ToArray();
            if (values.All(v => v == 0))
            {
                continue;
            }

            Scatter line = plot.Add.Scatter(_times, values, ToColor(category.Color));
            line.LineWidth = 1.5f;
            line.MarkerSize = 0;
        }

        Scatter total = plot.Add.Scatter(_times, points.Select(p => (double)p.Total).ToArray(),
            ToColor(AccountValueCategories.TotalColor));
        total.LineWidth = 3;
        total.MarkerSize = points.Count == 1 ? 6 : 0;

        _hoverLine = plot.Add.VerticalLine(_times[0], 1, AxisColor);
        _hoverLine.IsVisible = false;

        // The time axis spans the selected range, not just the points in it. Values from zero, so the categories'
        // scale reads honestly, with a little headroom above the total.
        double from = _model!.RangeStart.UtcDateTime.ToOADate();
        double to = _model.RangeEnd.UtcDateTime.ToOADate();
        if (to - from < 1.0 / 24)
        {
            from = to - 1.0 / 24;
        }

        plot.Axes.SetLimitsX(from, to);
        double max = points.Max(p => p.Total);
        plot.Axes.SetLimitsY(0, Math.Max(max * 1.08, 1));

        Plot.Refresh();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_model is not { HasData: true } || _hoverLine is null || _times.Length == 0)
        {
            return;
        }

        Point position = e.GetPosition(Plot);
        float scale = Plot.DisplayScale;
        Coordinates at = Plot.Plot.GetCoordinates(new Pixel((float)(position.X * scale), (float)(position.Y * scale)));
        int index = NearestIndex(at.X);
        AccountValuePoint point = _model.Points[index];

        ReadoutTime.Text = $"{point.Time.LocalDateTime:yyyy-MM-dd HH:mm}";
        ReadoutTotal.Copper = point.Total;
        Readout.IsVisible = true;
        Readout.Measure(Size.Infinity);
        Canvas.SetLeft(Readout, Math.Clamp(position.X - (Readout.DesiredSize.Width / 2), 0,
            Math.Max(0, Bounds.Width - Readout.DesiredSize.Width)));

        _hoverLine.X = _times[index];
        _hoverLine.IsVisible = true;
        Plot.Refresh();
    }

    private void HideReadout()
    {
        Readout.IsVisible = false;
        if (_hoverLine is { IsVisible: true })
        {
            _hoverLine.IsVisible = false;
            Plot.Refresh();
        }
    }

    private int NearestIndex(double x)
    {
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

    private static void Style(ScottPlot.Plot plot)
    {
        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = Colors.Transparent;
        plot.HideLegend();

        // Swap in the date axis before colouring: it replaces the bottom axis, which would otherwise keep ScottPlot's
        // default (black) labels.
        plot.Axes.DateTimeTicksBottom().TickGenerator = new LocalTimeTicks();

        plot.Axes.Color(AxisColor);
        plot.Grid.MajorLineColor = AxisColor.WithAlpha(0.15);
        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = FormatGold };
        plot.Axes.Bottom.TickLabelStyle.FontSize = 11;
        plot.Axes.Left.TickLabelStyle.FontSize = 11;
    }

    /// <summary>Axis label in whole gold (e.g. "1,250g"); account values are far too big for g/s/c labels.</summary>
    private static string FormatGold(double copper) => $"{copper / 10_000:N0}g";

    private static ScottPlot.Color ToColor(uint argb) =>
        new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));
}
