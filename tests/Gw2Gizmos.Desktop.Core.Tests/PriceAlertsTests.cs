namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>When a price alert triggers and re-arms, and how far it is from triggering.</summary>
public sealed class PriceAlertsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private static PriceAlert SellBelow(int price, bool repeat = false) =>
        new() { ItemId = 19976, ItemName = "Mystic Coin", Side = PriceAlertSide.Sell, Direction = PriceAlertDirection.Below, PriceCopper = price, Repeat = repeat };

    [Fact]
    public void Triggers_once_when_the_price_reaches_the_threshold()
    {
        (PriceAlert above, bool first) = PriceAlerts.Apply(SellBelow(10_000), 10_500, Now);
        Assert.False(first);
        Assert.False(above.IsTriggered);
        Assert.Equal(10_500, above.LastPrice);

        (PriceAlert reached, bool second) = PriceAlerts.Apply(above, 10_000, Now);
        Assert.True(second);
        Assert.True(reached.IsTriggered);
        Assert.Equal(Now, reached.TriggeredAtUtc);

        // Still below: no second notification.
        (PriceAlert stillBelow, bool third) = PriceAlerts.Apply(reached, 9_000, Now);
        Assert.False(third);
        Assert.True(stillBelow.IsTriggered);
    }

    [Fact]
    public void One_shot_alert_stays_triggered_when_the_price_moves_back()
    {
        (PriceAlert triggered, _) = PriceAlerts.Apply(SellBelow(10_000), 9_000, Now);
        (PriceAlert movedBack, bool notified) = PriceAlerts.Apply(triggered, 11_000, Now);

        Assert.False(notified);
        Assert.True(movedBack.IsTriggered);
    }

    [Fact]
    public void Repeating_alert_rearms_when_the_price_moves_back_and_triggers_again()
    {
        (PriceAlert triggered, _) = PriceAlerts.Apply(SellBelow(10_000, repeat: true), 9_000, Now);
        (PriceAlert rearmed, _) = PriceAlerts.Apply(triggered, 11_000, Now);
        Assert.False(rearmed.IsTriggered);

        (_, bool again) = PriceAlerts.Apply(rearmed, 9_500, Now);
        Assert.True(again);
    }

    [Fact]
    public void Empty_side_of_the_book_changes_nothing()
    {
        (PriceAlert alert, bool notified) = PriceAlerts.Apply(SellBelow(10_000), null, Now);

        Assert.False(notified);
        Assert.False(alert.IsTriggered);
        Assert.Null(alert.LastPrice);
        Assert.Equal(Now, alert.LastCheckedUtc);
    }

    [Fact]
    public void Above_alerts_and_distance()
    {
        var buyAbove = new PriceAlert { Side = PriceAlertSide.Buy, Direction = PriceAlertDirection.Above, PriceCopper = 5_000 };

        Assert.Equal(1_200, PriceAlerts.Distance(buyAbove with { LastPrice = 3_800 }));
        Assert.Null(PriceAlerts.Distance(buyAbove with { LastPrice = 5_000 })); // met
        Assert.Null(PriceAlerts.Distance(buyAbove)); // not checked yet
        Assert.Equal(300, PriceAlerts.Distance(SellBelow(10_000) with { LastPrice = 10_300 }));
        Assert.Equal("Buy price above", PriceAlerts.Condition(buyAbove));
    }

    [Fact]
    public void The_summary_says_when_there_are_no_alerts()
    {
        Assert.Equal("No alerts: add one from an item on the Items page", PriceAlerts.Summary([]));
    }

    [Fact]
    public void The_summary_names_the_waiting_alert_nearest_its_target()
    {
        PriceAlert far = SellBelow(100) with { ItemName = "Glob of Ectoplasm", LastPrice = 150 };
        PriceAlert near = SellBelow(100) with { LastPrice = 102 };
        PriceAlert triggered = SellBelow(100) with { IsTriggered = true, LastPrice = 90 };

        string summary = PriceAlerts.Summary([far, near, triggered]);

        Assert.Equal("3 alerts · 1 triggered · closest: Mystic Coin, 2% from its target", summary);
    }

    [Fact]
    public void The_summary_works_when_no_alert_is_waiting_with_a_price()
    {
        // Every alert triggered, or not checked yet: nothing to be nearest.
        PriceAlert triggered = SellBelow(100) with { IsTriggered = true, LastPrice = 90 };
        PriceAlert notChecked = SellBelow(100);

        Assert.Equal("2 alerts · 1 triggered", PriceAlerts.Summary([triggered, notChecked]));
    }
}

/// <summary>An alert's row only changes when something it shows changes.</summary>
public sealed class PriceAlertRowTests
{
    [Fact]
    public void Rows_differing_only_in_the_time_of_the_last_check_are_equal()
    {
        var checkedAlert = new PriceAlert { ItemId = 1, PriceCopper = 100, LastPrice = 120, LastCheckedUtc = DateTimeOffset.UnixEpoch };

        Assert.Equal(new PriceAlertRow(checkedAlert), new PriceAlertRow(checkedAlert with { LastCheckedUtc = DateTimeOffset.UtcNow }));
        Assert.NotEqual(new PriceAlertRow(checkedAlert), new PriceAlertRow(checkedAlert with { LastPrice = 110 }));
        Assert.NotEqual(new PriceAlertRow(checkedAlert), new PriceAlertRow(checkedAlert with { LastCheckedUtc = null }));
    }
}
