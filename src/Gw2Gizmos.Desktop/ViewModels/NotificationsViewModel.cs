using System;
using System.Collections.ObjectModel;
using System.Windows;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

public sealed class NotificationsViewModel : ViewModelBase
{
    public NotificationsViewModel(FileNotifier notifier, NotificationHub hub)
    {
        // History is stored newest-first, so it loads straight into the list.
        foreach (Notification notification in notifier.History())
        {
            Items.Add(notification);
        }

        hub.Added += OnAdded;
    }

    /// <summary>Newest first.</summary>
    public ObservableCollection<Notification> Items { get; } = new();

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
