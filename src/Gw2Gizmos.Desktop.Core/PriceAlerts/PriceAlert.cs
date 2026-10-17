using System;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Which trading-post price an alert watches: the highest buy order, or the lowest sell listing.</summary>
public enum PriceAlertSide
{
    Buy,
    Sell,
}

public enum PriceAlertDirection
{
    Below,
    Above,
}

/// <summary>
/// A price alert on one item: notify when its <see cref="Side"/> price goes <see cref="Direction"/> (at or past)
/// <see cref="PriceCopper"/>. Once triggered it stays triggered; with <see cref="Repeat"/> it re-arms when the price
/// moves back, so it can trigger again. Also carries the last price seen, for the alerts list.
/// </summary>
public sealed record PriceAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int ItemId { get; init; }

    public string ItemName { get; init; } = "";

    public PriceAlertSide Side { get; init; }

    public PriceAlertDirection Direction { get; init; }

    public int PriceCopper { get; init; }

    public bool Repeat { get; init; }

    public bool IsTriggered { get; init; }

    public DateTimeOffset? TriggeredAtUtc { get; init; }

    /// <summary>The watched price at the last check; null when that side of the book was empty (or not checked yet).</summary>
    public int? LastPrice { get; init; }

    public DateTimeOffset? LastCheckedUtc { get; init; }
}

/// <summary>The rules for when a <see cref="PriceAlert"/> triggers and re-arms, and how it reads.</summary>
public static class PriceAlerts
{
    /// <summary>Whether <paramref name="price"/> satisfies the alert's condition (reaching the threshold counts).</summary>
    public static bool IsMet(PriceAlert alert, int price) =>
        alert.Direction == PriceAlertDirection.Below ? price <= alert.PriceCopper : price >= alert.PriceCopper;

    /// <summary>
    /// Applies a newly polled price: an armed alert whose condition is now met triggers (the caller notifies); a
    /// triggered repeating alert whose condition no longer holds re-arms. A null price (empty side of the book)
    /// changes nothing but the last-seen price.
    /// </summary>
    public static (PriceAlert Alert, bool Triggered) Apply(PriceAlert alert, int? price, DateTimeOffset now)
    {
        PriceAlert checkedAlert = alert with { LastPrice = price, LastCheckedUtc = now };
        if (price is not { } current)
        {
            return (checkedAlert, false);
        }

        bool met = IsMet(alert, current);
        if (met && !alert.IsTriggered)
        {
            return (checkedAlert with { IsTriggered = true, TriggeredAtUtc = now }, true);
        }

        if (!met && alert.IsTriggered && alert.Repeat)
        {
            return (checkedAlert with { IsTriggered = false }, false);
        }

        return (checkedAlert, false);
    }

    /// <summary>How far the last price is from triggering the alert, in copper; null when met or unknown.</summary>
    public static long? Distance(PriceAlert alert) =>
        alert.LastPrice is not { } price || IsMet(alert, price)
            ? null
            : Math.Abs((long)price - alert.PriceCopper);

    /// <summary>E.g. "Sell price below" — followed in the UI by the threshold.</summary>
    public static string Condition(PriceAlert alert) =>
        $"{(alert.Side == PriceAlertSide.Sell ? "Sell" : "Buy")} price {(alert.Direction == PriceAlertDirection.Below ? "below" : "above")}";
}
