using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Each account's goals, in the order the account wants them met, persisted as JSON in a per-user file, cached in
/// memory and written through on each change. Added from the Items page; edited on the Goals page.
/// </summary>
public sealed class GoalStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private List<Goal> _goals;

    public GoalStore(AppPaths paths)
    {
        _path = paths.File("goals.json");
        _goals = Load();
    }

    /// <summary>Raised after any change (on the thread that made it).</summary>
    public event Action? Changed;

    /// <summary>An account's goals, first to be met first.</summary>
    public IReadOnlyList<Goal> For(string accountId)
    {
        lock (_gate)
        {
            return _goals.Where(g => g.AccountId == accountId).ToArray();
        }
    }

    /// <summary>Adds an item as the account's last goal, or one more of it when it's a goal already.</summary>
    public void Add(string accountId, int itemId, string itemName) => Mutate(goals =>
    {
        int index = goals.FindIndex(g => g.AccountId == accountId && g.ItemId == itemId);
        if (index >= 0)
        {
            goals[index] = goals[index] with { Count = goals[index].Count + 1 };
        }
        else
        {
            goals.Add(new Goal { AccountId = accountId, ItemId = itemId, ItemName = itemName });
        }
    });

    public void SetCount(Guid id, int count) => Mutate(goals =>
    {
        int index = goals.FindIndex(g => g.Id == id);
        if (index >= 0)
        {
            goals[index] = goals[index] with { Count = Math.Max(1, count) };
        }
    });

    /// <summary>Moves a goal before (<paramref name="offset"/> -1) or after (+1) the account's next goal.</summary>
    public void Move(Guid id, int offset) => Mutate(goals =>
    {
        int index = goals.FindIndex(g => g.Id == id);
        if (index < 0)
        {
            return;
        }

        Goal goal = goals[index];
        List<int> accountIndexes = goals
            .Select((g, i) => (g, i))
            .Where(pair => pair.g.AccountId == goal.AccountId)
            .Select(pair => pair.i)
            .ToList();
        int position = accountIndexes.IndexOf(index) + offset;
        if (position < 0 || position >= accountIndexes.Count)
        {
            return;
        }

        int other = accountIndexes[position];
        (goals[index], goals[other]) = (goals[other], goals[index]);
    });

    public void Remove(Guid id) => Mutate(goals => goals.RemoveAll(g => g.Id == id));

    private void Mutate(Action<List<Goal>> change)
    {
        lock (_gate)
        {
            change(_goals);
            File.WriteAllText(_path, JsonSerializer.Serialize(_goals));
        }

        Changed?.Invoke();
    }

    private List<Goal> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<Goal>>(File.ReadAllText(_path)) ?? new List<Goal>()
                : new List<Goal>();
        }
        catch (Exception)
        {
            return new List<Goal>();
        }
    }
}
