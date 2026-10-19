using System.Collections;
using System.Collections.Specialized;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The Logs page shows its own copy of the app-wide log, kept in step while open and let go when it closes, so nothing
/// keeps following the shared list after the page is gone (that once crashed the app overnight).
/// </summary>
public sealed class LogsViewModelTests
{
    private static LogEntry Line(int n) => new(DateTimeOffset.UnixEpoch.AddSeconds(n), "INF", "Desktop", $"line {n}");

    [Fact]
    public void The_page_follows_the_log_while_open_including_trimming()
    {
        var store = new LogStore(new InlineDispatcher());
        store.Add(Line(1));
        var views = new CapturingViewFactory();
        var logs = new LogsViewModel(store, views);

        for (int n = 2; n <= 1005; n++)
        {
            store.Add(Line(n));
        }

        Assert.Equal(store.Entries, views.Source!.Cast<LogEntry>());
        Assert.Equal(1000, views.Source!.Count);
        Assert.Equal("line 1005", ((LogEntry)views.Source![0]!).Message);
        GC.KeepAlive(logs);
    }

    [Fact]
    public void After_leaving_the_page_its_copy_no_longer_follows_the_log()
    {
        var store = new LogStore(new InlineDispatcher());
        var views = new CapturingViewFactory();
        var logs = new LogsViewModel(store, views);
        store.Add(Line(1));

        logs.OnNavigatedFrom();
        var changes = 0;
        ((INotifyCollectionChanged)views.Source!).CollectionChanged += (_, _) => changes++;
        store.Add(Line(2));

        Assert.Equal(0, changes);
        Assert.Single(views.Source!);
    }

    [Fact]
    public void A_failure_in_something_following_the_log_doesnt_escape()
    {
        var store = new LogStore(new InlineDispatcher());
        store.Entries.CollectionChanged += (_, _) => throw new ArgumentOutOfRangeException("index");

        store.Add(Line(1)); // would have thrown out of the UI thread and ended the app

        Assert.Single(store.Entries);
    }

    /// <summary>Records the list the page's view was made over.</summary>
    private sealed class CapturingViewFactory : IItemsViewFactory
    {
        public IList? Source { get; private set; }

        public IItemsView Create(IList source)
        {
            Source = source;
            return new ListView(source);
        }

        public IItemsView CreateLiveSorted(IList source, IReadOnlyList<ItemsViewSort> sorts) => Create(source);

        private sealed class ListView(IList source) : IItemsView
        {
            public Predicate<object>? Filter { get; set; }

            public void Refresh() { }

            public IEnumerator GetEnumerator() => source.GetEnumerator();
        }
    }

    [Fact]
    public void Coming_back_to_the_page_catches_up_and_follows_the_log_again()
    {
        var store = new LogStore(new InlineDispatcher());
        var views = new CapturingViewFactory();
        var logs = new LogsViewModel(store, views);
        logs.OnNavigatedFrom();
        store.Add(Line(1));

        logs.OnNavigatedTo();
        store.Add(Line(2));

        Assert.Equal(["line 2", "line 1"], views.Source!.Cast<LogEntry>().Select(entry => entry.Message));
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public void Post(Action action) => action();

        public void Invoke(Action action) => action();

        public IUiTimer CreateTimer(TimeSpan interval, Action tick) => throw new NotSupportedException();
    }
}
