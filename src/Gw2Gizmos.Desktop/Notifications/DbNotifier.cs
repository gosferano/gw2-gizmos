using System;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Notifications;
using Gw2Gizmos.Data.Worker.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Single-purpose <see cref="INotifier"/> that persists each notification to the shared
/// <c>Notifications</c> table (tagged <c>Source = Desktop</c>) and publishes it to the in-app feed.
/// </summary>
public sealed class DbNotifier : INotifier
{
    public const string SourceName = "Desktop";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationHub _hub;

    public DbNotifier(IServiceScopeFactory scopeFactory, NotificationHub hub)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
    }

    public void Notify(string title, string message, string category = "General")
    {
        var notification = new Notification
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Source = SourceName,
            Category = category,
            Title = title,
            Body = message,
            IsRead = false,
        };

        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            dbContext.Notifications.Add(notification);
            dbContext.SaveChanges();
        }

        _hub.Publish(notification);
    }
}
