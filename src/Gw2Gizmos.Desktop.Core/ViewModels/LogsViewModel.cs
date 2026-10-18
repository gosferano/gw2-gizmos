using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Logs page: the recent log lines, filterable by text, level and process. The page shows its own copy of
/// the app-wide <see cref="LogStore"/>, kept in step while the page is open and let go when it closes — a view left
/// following the shared, ever-changing list after its page was gone once crashed the app overnight.
/// </summary>
public sealed class LogsViewModel : ViewModelBase, INavigationAware
{
    private readonly LogStore _store;
    private readonly ObservableCollection<LogEntry> _entries;
    private readonly IItemsView _view;
    private string _filterText = "";
    private string _levelFilter = "All";
    private bool _showDesktop = true;
    private bool _showWorker = true;

    public LogsViewModel(LogStore store, IItemsViewFactory views)
    {
        _store = store;
        _entries = new ObservableCollection<LogEntry>(store.Entries);
        _view = views.Create(_entries);
        _view.Filter = Matches;
        store.Entries.CollectionChanged += OnStoreChanged;
    }

    public void OnNavigatedFrom() => _store.Entries.CollectionChanged -= OnStoreChanged;

    public IItemsView Entries => _view;

    public string[] Levels { get; } = ["All", "WRN+", "ERR"];

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                _view.Refresh();
            }
        }
    }

    public string LevelFilter
    {
        get => _levelFilter;
        set
        {
            if (SetProperty(ref _levelFilter, value))
            {
                _view.Refresh();
            }
        }
    }

    public bool ShowDesktop
    {
        get => _showDesktop;
        set
        {
            if (SetProperty(ref _showDesktop, value))
            {
                _view.Refresh();
            }
        }
    }

    public bool ShowWorker
    {
        get => _showWorker;
        set
        {
            if (SetProperty(ref _showWorker, value))
            {
                _view.Refresh();
            }
        }
    }

    /// <summary>Applies the store's change to the page's copy (the store only adds at the top and trims the bottom).</summary>
    private void OnStoreChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        _entries.Insert(e.NewStartingIndex + i, (LogEntry)e.NewItems[i]!);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        _entries.RemoveAt(e.OldStartingIndex);
                    }

                    break;
                default:
                    Resync();
                    break;
            }
        }
        catch (Exception)
        {
            // The grid's view choked on an incremental change: start the copy over rather than let it escape.
            Resync();
        }
    }

    private void Resync()
    {
        _entries.Clear();
        foreach (LogEntry entry in _store.Entries)
        {
            _entries.Add(entry);
        }
    }

    private bool Matches(object obj)
    {
        var entry = (LogEntry)obj;

        if (entry.Source == "Desktop" && !_showDesktop)
        {
            return false;
        }

        if (entry.Source == "Worker" && !_showWorker)
        {
            return false;
        }

        if (_levelFilter == "WRN+" && entry.Level is not ("WRN" or "ERR" or "FTL"))
        {
            return false;
        }

        if (_levelFilter == "ERR" && entry.Level is not ("ERR" or "FTL"))
        {
            return false;
        }

        if (
            !string.IsNullOrWhiteSpace(_filterText)
            && !entry.Message.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return true;
    }
}
