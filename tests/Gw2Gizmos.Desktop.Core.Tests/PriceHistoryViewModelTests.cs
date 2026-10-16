namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The price-history window and navigator drag rules, shared by every UI head: default window, click-to-centre,
/// panning that keeps its width at the ends, and resizing down to the minimum window.
/// </summary>
public class PriceHistoryViewModelTests
{
    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0);

    [Fact]
    public void Load_ShowsTheMostRecentQuarter()
    {
        PriceHistoryViewModel history = Loaded(days: 8);

        Assert.True(history.HasData);
        Assert.Equal(Start.AddDays(6), history.WindowStart);
        Assert.Equal(Start.AddDays(8), history.WindowEnd);
    }

    [Fact]
    public void Load_WithFewerThanTwoPoints_HasNoData()
    {
        var history = new PriceHistoryViewModel();
        history.Load(new[] { new HistoryPoint(Start, 1, 2, 3) });

        Assert.False(history.HasData);
        Assert.Empty(history.Points);
    }

    [Fact]
    public void ClickOutsideTheWindow_CentresItOnTheClick_KeepingItsWidth()
    {
        PriceHistoryViewModel history = Loaded(days: 8); // window: days 6-8 (2 days wide)

        history.BeginDrag(Start.AddDays(3));

        Assert.Equal(Start.AddDays(2), history.WindowStart);
        Assert.Equal(Start.AddDays(4), history.WindowEnd);
    }

    [Fact]
    public void Panning_PastTheStart_StopsAtTheStart_KeepingItsWidth()
    {
        PriceHistoryViewModel history = Loaded(days: 8);

        history.BeginDrag(Start.AddDays(7)); // middle of the window → pan
        history.DragTo(Start.AddDays(-5));
        history.EndDrag();

        Assert.Equal(Start, history.WindowStart);
        Assert.Equal(Start.AddDays(2), history.WindowEnd);
    }

    [Fact]
    public void ResizingTheStartEdge_CannotShrinkBelowTheMinimumWindow()
    {
        PriceHistoryViewModel history = Loaded(days: 8);

        history.BeginDrag(Start.AddDays(6)); // on the left edge → resize start
        history.DragTo(Start.AddDays(9));    // past the right edge

        Assert.Equal(Start.AddDays(8), history.WindowEnd);
        Assert.Equal(TimeSpan.FromMinutes(15), history.WindowEnd - history.WindowStart);
    }

    [Fact]
    public void DraggingWithoutBegin_DoesNothing()
    {
        PriceHistoryViewModel history = Loaded(days: 8);

        history.DragTo(Start.AddDays(1));

        Assert.Equal(Start.AddDays(6), history.WindowStart);
    }

    /// <summary>Hourly points across <paramref name="days"/> days.</summary>
    private static PriceHistoryViewModel Loaded(int days)
    {
        var points = Enumerable.Range(0, (days * 24) + 1)
            .Select(hour => new HistoryPoint(Start.AddHours(hour), 100 + hour, 120 + hour, hour))
            .ToArray();
        var history = new PriceHistoryViewModel();
        history.Load(points);
        return history;
    }
}
