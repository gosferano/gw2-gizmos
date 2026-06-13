using System.Collections.Generic;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The notification categories the app produces, with display text for the Notifications settings page.
/// Each <see cref="NotificationCategoryInfo.Key"/> must match the category string a producer passes to
/// <c>INotifier.Notify</c> (the event reminder service uses <see cref="Events"/>, the delivery poller uses
/// <see cref="Delivery"/>). A category not listed here is treated as enabled.
/// </summary>
public static class NotificationCategories
{
    public const string Events = "Events";
    public const string Delivery = "Delivery";

    public static IReadOnlyList<NotificationCategoryInfo> All { get; } = new[]
    {
        new NotificationCategoryInfo(Events, "Event reminders", "Toasts before a subscribed event begins."),
        new NotificationCategoryInfo(
            Delivery,
            "Trading-post delivery",
            "Toasts when coins or items arrive in your delivery box."
        ),
    };
}

/// <summary>A notification category and its display text for the settings page.</summary>
public sealed record NotificationCategoryInfo(string Key, string Name, string Description);
