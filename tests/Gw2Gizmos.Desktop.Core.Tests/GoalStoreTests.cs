namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Each account's goals, in order, kept across restarts.</summary>
public sealed class GoalStoreTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-goals-{Guid.NewGuid():N}");

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    private GoalStore NewStore() => new(new AppPaths(_dataDirectory));

    [Fact]
    public void Adding_a_goal_again_adds_one_more_of_it()
    {
        GoalStore store = NewStore();
        store.Add("acc", 1, "Twilight");
        store.Add("acc", 1, "Twilight");
        store.Add("other", 1, "Twilight");

        Goal goal = Assert.Single(store.For("acc"));
        Assert.Equal(2, goal.Count);
        Assert.Single(store.For("other"));
    }

    [Fact]
    public void Goals_move_among_the_accounts_own_and_are_kept_across_restarts()
    {
        GoalStore store = NewStore();
        store.Add("acc", 1, "A");
        store.Add("other", 9, "X");
        store.Add("acc", 2, "B");
        store.Add("acc", 3, "C");

        store.Move(store.For("acc")[2].Id, -1);
        store.Move(store.For("acc")[0].Id, -1); // already first

        Assert.Equal([1, 3, 2], NewStore().For("acc").Select(g => g.ItemId));
        Assert.Equal([9], NewStore().For("other").Select(g => g.ItemId));
    }
}
