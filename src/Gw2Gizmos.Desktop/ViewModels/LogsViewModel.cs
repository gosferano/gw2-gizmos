using System;
using System.ComponentModel;
using System.Windows.Data;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

public sealed class LogsViewModel : ViewModelBase
{
    private readonly ICollectionView _view;
    private string _filterText = "";
    private string _levelFilter = "All";
    private bool _showDesktop = true;
    private bool _showWorker = true;

    public LogsViewModel(LogStore store)
    {
        _view = CollectionViewSource.GetDefaultView(store.Entries);
        _view.Filter = Matches;
    }

    public ICollectionView Entries => _view;

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
