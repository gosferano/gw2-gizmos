using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Bounded, UI-bindable buffer of recent log lines from both processes (the app's in-memory sink and
/// the tailed worker file). Adds are marshalled to the UI thread; the oldest entries drop once the
/// cap is reached, so memory stays flat.
/// </summary>
public sealed class LogStore
{
    private const int Capacity = 1000;

    private readonly IUiDispatcher _dispatcher;

    public LogStore(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Add(LogEntry entry)
    {
        if (_dispatcher.CheckAccess())
        {
            AddCore(entry);
        }
        else
        {
            _dispatcher.Post(() => AddCore(entry));
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

        if (_dispatcher.CheckAccess())
        {
            AddRangeCore(entries);
        }
        else
        {
            _dispatcher.Post(() => AddRangeCore(entries));
        }
    }

    private void AddCore(LogEntry entry) => AddRangeCore([entry]);

    private void AddRangeCore(IReadOnlyList<LogEntry> entries)
    {
        // Keeping a log must never take the app down: a failure here (in whatever follows the list) is dropped — it
        // can't be logged, since this is the log.
        try
        {
            // Insert each at the front so the batch's last (newest) line ends up on top; the oldest drop off the end.
            foreach (LogEntry entry in entries)
            {
                Entries.Insert(0, entry);
            }

            while (Entries.Count > Capacity)
            {
                Entries.RemoveAt(Entries.Count - 1);
            }
        }
        catch (Exception)
        {
        }
    }
}
