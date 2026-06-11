using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Notifications;
using Gw2Gizmos.Herald.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Herald;

public sealed class NotificationsViewModel : ViewModelBase
{
    private const int HistoryLimit = 200;

    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationsViewModel(IServiceScopeFactory scopeFactory, NotificationHub hub)
    {
        _scopeFactory = scopeFactory;
        LoadHistory();
        hub.Added += OnAdded;
    }

    /// <summary>Newest first.</summary>
    public ObservableCollection<Notification> Items { get; } = new();

    private void LoadHistory()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        foreach (Notification notification in dbContext.Notifications.OrderByDescending(n => n.Id).Take(HistoryLimit))
        {
            Items.Add(notification);
        }
    }

    private void OnAdded(Notification notification)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Items.Insert(0, notification);
        }
        else
        {
            dispatcher.BeginInvoke(new Action(() => Items.Insert(0, notification)));
        }
    }
}
