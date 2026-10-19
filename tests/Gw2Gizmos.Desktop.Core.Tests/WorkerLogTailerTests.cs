using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>The worker's log lines become entries for the Logs page, a multi-line entry (an exception) as
/// one.</summary>
public sealed class WorkerLogTailerTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tailer-{Guid.NewGuid():N}");

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    private WorkerLogTailer NewTailer() =>
        new(new LogStore(new InlineDispatcher()), new AppPaths(_dataDirectory));

    [Fact]
    public void An_exception_and_its_stack_trace_stay_with_their_entry_in_order()
    {
        List<LogEntry> entries = NewTailer().Parse(
            [
                "22:33:10.916 [INF] [production|Recipes] Processing recipes 1401 to 1600",
                "22:33:28.195 [ERR] [production|Recipes] Error processing recipes 1401 to 1600",
                "System.InvalidOperationException: The instance cannot be tracked",
                "   at IdentityMap.ThrowIdentityConflict(InternalEntityEntry entry)",
                "   at RecipesUpdater.UpdateRecipes(CancellationToken stoppingToken)",
                "22:33:28.200 [INF] [production|Recipes] Processing recipes 1601 to 1800",
            ]
        );

        Assert.Equal(3, entries.Count);
        LogEntry error = entries[1];
        Assert.Equal("ERR", error.Level);
        Assert.Equal("Error processing recipes 1401 to 1600", error.Message);
        Assert.Equal(
            "System.InvalidOperationException: The instance cannot be tracked\n"
                + "   at IdentityMap.ThrowIdentityConflict(InternalEntityEntry entry)\n"
                + "   at RecipesUpdater.UpdateRecipes(CancellationToken stoppingToken)",
            error.Details
        );
        Assert.Equal("+3 lines", error.DetailsSummary);
        Assert.False(entries[0].HasDetails);
        Assert.Equal("Processing recipes 1601 to 1800", entries[2].Message);
    }

    [Fact]
    public void Lines_continuing_an_entry_from_an_earlier_read_form_their_own_entry_at_its_level()
    {
        WorkerLogTailer tailer = NewTailer();
        tailer.Parse(["22:33:28.195 [ERR] [production|Recipes] Error processing recipes"]);

        List<LogEntry> entries = tailer.Parse(["   at A.B()", "   at C.D()"]);

        LogEntry rest = Assert.Single(entries);
        Assert.Equal("ERR", rest.Level);
        Assert.Equal("   at A.B()", rest.Message);
        Assert.Equal("   at C.D()", rest.Details);
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public void Post(Action action) => action();

        public void Invoke(Action action) => action();

        public IUiTimer CreateTimer(TimeSpan interval, Action tick) => throw new NotSupportedException();
    }
}
