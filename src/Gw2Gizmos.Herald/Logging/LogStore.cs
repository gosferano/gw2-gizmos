using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Bounded, UI-bindable buffer of recent log lines from both processes (Herald's in-memory sink and
/// the tailed worker file). Adds are marshalled to the UI thread; the oldest entries drop once the
/// cap is reached, so memory stays flat.
/// </summary>
public sealed class LogStore
{
    private const int Capacity = 1000;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Add(LogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            AddCore(entry);
        }
        else
        {
            dispatcher.BeginInvoke(new Action(() => AddCore(entry)));
        }
    }

    /// <summary>Adds a batch with a single UI-thread hop — used by the worker tailer to avoid a
    /// per-line dispatch flood during verbose ingestion.</summary>
    public void AddRange(IReadOnlyList<LogEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            AddRangeCore(entries);
        }
        else
        {
            dispatcher.BeginInvoke(new Action(() => AddRangeCore(entries)));
        }
    }

    private void AddCore(LogEntry entry)
    {
        // Newest first (top of the list); the oldest drops off the end.
        Entries.Insert(0, entry);
        if (Entries.Count > Capacity)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }

    private void AddRangeCore(IReadOnlyList<LogEntry> entries)
    {
        // Insert each at the front so the batch's last (newest) line ends up on top.
        foreach (LogEntry entry in entries)
        {
            Entries.Insert(0, entry);
        }

        while (Entries.Count > Capacity)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
}
