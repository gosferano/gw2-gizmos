using Gw2Gizmos.Desktop.Core;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Date ticks for a chart plotted in UTC (OA dates), chosen and labelled in local time: ScottPlot's automatic date
/// ticks run over the local span, then each tick moves back to where that local time falls in UTC. Plotting in UTC
/// keeps time moving forward through daylight saving changes (the hour repeated when the clocks go back would
/// otherwise overlap itself), while midnight ticks still land on local midnight. A local time skipped when the clocks
/// go forward gets no tick.
/// </summary>
public sealed class LocalTimeTicks : IDateTimeTickGenerator
{
    private readonly DateTimeAutomatic _local = new() { LabelFormatter = PriceHistoryViewModel.FormatTime };
    private readonly TimeZoneInfo _zone;

    public LocalTimeTicks(TimeZoneInfo? zone = null) => _zone = zone ?? TimeZoneInfo.Local;

    public Tick[] Ticks { get; set; } = Array.Empty<Tick>();

    public int MaxTickCount
    {
        get => _local.MaxTickCount;
        set => _local.MaxTickCount = value;
    }

    public void Regenerate(CoordinateRange range, Edge edge, PixelLength size, Paint paint, LabelStyle labelStyle)
    {
        _local.Regenerate(new CoordinateRange(ToLocal(range.Min), ToLocal(range.Max)), edge, size, paint, labelStyle);
        Ticks = _local.Ticks
            .Select(tick => ToUtc(tick.Position) is { } utc ? new Tick(utc, tick.Label, tick.IsMajor) : (Tick?)null)
            .OfType<Tick>()
            .ToArray();
    }

    public IEnumerable<double> ConvertToCoordinateSpace(IEnumerable<DateTime> dates) =>
        _local.ConvertToCoordinateSpace(dates);

    /// <summary>The local time (OA date) of a UTC OA date.</summary>
    private double ToLocal(double utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(FromOADate(utc), DateTimeKind.Utc), _zone).ToOADate();

    /// <summary>The UTC OA date of a local OA date, or null for a local time the clocks skipped.</summary>
    private double? ToUtc(double local)
    {
        DateTime time = DateTime.SpecifyKind(FromOADate(local), DateTimeKind.Unspecified);
        return _zone.IsInvalidTime(time) ? null : TimeZoneInfo.ConvertTimeToUtc(time, _zone).ToOADate();
    }

    private static DateTime FromOADate(double oaDate) =>
        DateTime.FromOADate(Math.Clamp(oaDate, DateTime.MinValue.ToOADate() + 2, DateTime.MaxValue.ToOADate() - 2));
}
