using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A goal in the Goals page's list, or the "All goals" entry above them (<see cref="IsAll"/>): its progress and what's
/// left to buy. A goal's count is edited in place.
/// </summary>
public sealed class GoalRow : ViewModelBase
{
    private readonly GoalStore? _store;
    private Goal _goal;
    private GoalNeeds? _needs;

    public GoalRow(Goal goal, GoalStore? store)
    {
        _goal = goal;
        _store = store;
    }

    /// <summary>The "All goals" entry: every goal's needs together.</summary>
    public bool IsAll => _store is null;

    public bool IsGoal => !IsAll;

    public Guid Id => _goal.Id;

    public int ItemId => _goal.ItemId;

    public string Name => IsAll ? "All goals" : _goal.ItemName;

    /// <summary>How many of the item; typing a new count saves it.</summary>
    public decimal? Count
    {
        get => _goal.Count;
        set
        {
            if (value is { } count && (int)count != _goal.Count && count >= 1)
            {
                _goal = _goal with { Count = (int)count };
                _store?.SetCount(_goal.Id, (int)count);
            }
        }
    }

    public GoalNeeds? Needs
    {
        get => _needs;
        set
        {
            _needs = value;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(BuyCopper));
            OnPropertyChanged(nameof(HasBuy));
        }
    }

    /// <summary>The share of the goal's cost holdings cover, 0 to 100.</summary>
    public double Progress => (Needs?.Progress ?? 0) * 100;

    /// <summary>E.g. "62% covered · 3 parts can't be bought", or "Done".</summary>
    public string Status
    {
        get
        {
            if (Needs is not { } needs)
            {
                return "Working it out…";
            }

            if (needs.Done)
            {
                return "Done: your holdings cover it";
            }

            int cantBuy = needs.CantBuy.Count;
            return string.Join(" · ", new[]
            {
                $"{needs.Progress:P0} covered",
                cantBuy > 0 ? $"{cantBuy} {(cantBuy == 1 ? "part" : "parts")} can't be bought" : null,
            }.Where(part => part is not null));
        }
    }

    public long BuyCopper => Needs?.BuyCopper ?? 0;

    public bool HasBuy => BuyCopper > 0;

    internal void Update(Goal goal)
    {
        bool countChanged = goal.Count != _goal.Count;
        _goal = goal;
        if (countChanged)
        {
            OnPropertyChanged(nameof(Count));
        }
    }
}

/// <summary>A section of the selected goal's needs: to buy, from vendors, currencies, can't be bought, covered.
/// </summary>
public sealed record GoalHeading(string Title, string Description, long Copper)
{
    public bool HasCopper => Copper > 0;
}

/// <summary>A line of the selected goal's needs: how many of an item, and what they cost or how they're had.</summary>
public sealed record GoalLineRow(GoalLine Line)
{
    public int ItemId => Line.ItemId;

    public string Name => Line.Name;

    public string CountText => $"× {Line.Count.ToString("N0", CultureInfo.CurrentCulture)}";

    public long Copper => Line.Copper;

    public bool HasCopper => Line.Copper > 0;

    public string? Note => Line.Note;

    public bool HasNote => !string.IsNullOrEmpty(Line.Note);
}

/// <summary>A currency the selected goal needs, against what the account has towards it.</summary>
public sealed record GoalCurrencyRow(GoalCurrency Currency)
{
    public string Name => Currency.Name;

    public int ItemId => Currency.ItemId ?? 0;

    public bool IsItem => Currency.ItemId is not null;

    public string? IconUrl => Currency.IconUrl;

    /// <summary>E.g. "2,000 needed · you have 1,500 · 500 short".</summary>
    public string Text => string.Join(" · ", new[]
    {
        $"{Number(Currency.Needed)} needed",
        $"you have {Number(Currency.Have)}",
        Currency.Missing > 0 ? $"{Number(Currency.Missing)} short" : null,
    }.Where(part => part is not null));

    private static string Number(long value) => value.ToString("N0", CultureInfo.CurrentCulture);
}

/// <summary>
/// Backs the Advisor › Goals page: the items the account plans to make or buy, what each still needs from its holdings
/// (<see cref="GoalPlanner"/>) and what that costs, against the coin it has. Goals are added from the Items page and
/// kept in order; the first gets a material both need. Re-plans after syncs and when goals change, and rebuilds the
/// recipe trees after price polls.
/// </summary>
public sealed class GoalsViewModel : ViewModelBase, INavigationAware
{
    private readonly GoalStore _store;
    private readonly AccountReader _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly Dictionary<int, RecipeNode> _trees = new();
    private readonly GoalRow _all = new(new Goal(), store: null);
    private Task<RecipeTreeBuilder>? _builder;
    private GoalHoldings _holdings = GoalHoldings.Empty;
    private int _version;
    private GoalRow? _selected;
    private bool _loaded;
    private long _coinCopper;
    private long _buyCopper;

    public GoalsViewModel(
        GoalStore store,
        AccountReader reader,
        IServiceScopeFactory scopeFactory,
        SelectedAccountService selected,
        DataChangeMonitor dataChanges,
        INavigationService navigation
    )
    {
        _store = store;
        _reader = reader;
        _scopeFactory = scopeFactory;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = AdvisorBreadcrumbs.Section("Goals");
        AddCommand = new RelayCommand(() => navigation.NavigateTo(AppPage.Items));
        RemoveCommand = new RelayCommand<GoalRow>(row =>
        {
            if (row is { IsGoal: true })
            {
                _store.Remove(row.Id);
            }
        });
        MoveUpCommand = new RelayCommand<GoalRow>(row => Move(row, -1));
        MoveDownCommand = new RelayCommand<GoalRow>(row => Move(row, +1));
        _store.Changed += OnGoalsChanged;
        _ = LoadAsync(holdings: true, trees: false);
        _dataChanges = dataChanges.Subscribe(changes =>
        {
            // Prices move the cheapest path, so the trees are rebuilt; holdings move what's covered.
            bool prices = (changes & DataChanges.Catalog) != 0;
            if (prices || (changes & (DataChanges.AccountSync | DataChanges.Holdings)) != 0)
            {
                _ = LoadAsync(holdings: true, trees: prices);
            }
        });
    }

    public void OnNavigatedFrom()
    {
        _store.Changed -= OnGoalsChanged;
        _dataChanges.Dispose();
    }

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    /// <summary>Opens the Items page, where an item is added as a goal.</summary>
    public RelayCommand AddCommand { get; }

    public RelayCommand<GoalRow> RemoveCommand { get; }

    public RelayCommand<GoalRow> MoveUpCommand { get; }

    public RelayCommand<GoalRow> MoveDownCommand { get; }

    /// <summary>"All goals" first, then each goal in the order it's met.</summary>
    public ObservableCollection<GoalRow> Rows { get; } = new();

    public bool HasGoals => Rows.Count > 1;

    public bool NoGoals => HasAccount && _loaded && !HasGoals;

    public GoalRow? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                ShowNeeds();
            }
        }
    }

    /// <summary>The selected goal's needs, section by section: headings and their lines.</summary>
    public IReadOnlyList<object> Details { get; private set; } = [];

    /// <summary>What buying everything the goals still need costs.</summary>
    public long BuyCopper
    {
        get => _buyCopper;
        private set => SetProperty(ref _buyCopper, value);
    }

    /// <summary>The coin in the account's wallet.</summary>
    public long CoinCopper
    {
        get => _coinCopper;
        private set => SetProperty(ref _coinCopper, value);
    }

    /// <summary>How much more coin buying it all takes, or 0 when the wallet covers it.</summary>
    public long ShortCopper => Math.Max(0, BuyCopper - CoinCopper);

    public bool IsShort => ShortCopper > 0;

    public bool CoinCovers => HasGoals && !IsShort;

    private void Move(GoalRow? row, int offset)
    {
        if (row is { IsGoal: true })
        {
            _store.Move(row.Id, offset);
        }
    }

    private void OnGoalsChanged() => _ = LoadAsync(holdings: false, trees: false);

    private async Task LoadAsync(bool holdings, bool trees)
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        int version = ++_version;
        IReadOnlyList<Goal> goals = _store.For(accountId);
        ShowGoals(goals);
        try
        {
            if (trees)
            {
                _builder = null;
            }

            Task<RecipeTreeBuilder> builder = _builder ??= BuildBuilderAsync();
            Dictionary<int, RecipeNode> known = trees ? new() : new(_trees);
            (GoalPlan plan, GoalHoldings held, Dictionary<int, RecipeNode> built) = await Task.Run(async () =>
            {
                GoalHoldings read = holdings ? _reader.GetGoalHoldings(accountId) : _holdings;
                var built = new Dictionary<int, RecipeNode>(known);
                foreach (int itemId in goals.Select(g => g.ItemId).Distinct().Where(id => !built.ContainsKey(id)))
                {
                    built[itemId] = await (await builder).BuildTreeAsync(itemId, CancellationToken.None);
                }

                GoalPlan plan = GoalPlanner.Plan(goals.Select(g => (g, built[g.ItemId])).ToList(), read.Items,
                    read.Wallet);
                return (plan, read, built);
            });

            if (version != _version)
            {
                return;
            }

            _holdings = held;
            _trees.Clear();
            foreach ((int itemId, RecipeNode tree) in built)
            {
                _trees[itemId] = tree;
            }

            ShowPlan(plan);
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    // The recipe-tree builder, loaded once in the background and reused for every goal (memoized, in memory).
    private Task<RecipeTreeBuilder> BuildBuilderAsync() =>
        Task.Run(async () =>
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            var builder = new RecipeTreeBuilder(dbContext, Configuration.Default, _accountId);
            await builder.EnsureLoadedAsync(CancellationToken.None);
            return builder;
        });

    // Keeps the rows of goals still there (so a count being typed isn't lost), adds new ones, and drops removed ones,
    // in the store's order.
    private void ShowGoals(IReadOnlyList<Goal> goals)
    {
        Dictionary<Guid, GoalRow> existing = Rows.Where(r => r.IsGoal).ToDictionary(r => r.Id);
        var wanted = new List<GoalRow> { _all };
        foreach (Goal goal in goals)
        {
            if (existing.TryGetValue(goal.Id, out GoalRow? row))
            {
                row.Update(goal);
            }
            else
            {
                row = new GoalRow(goal, _store);
            }

            wanted.Add(row);
        }

        if (!Rows.SequenceEqual(wanted))
        {
            GoalRow? selected = Selected;
            Rows.Clear();
            foreach (GoalRow row in wanted)
            {
                Rows.Add(row);
            }

            Selected = selected is not null && Rows.Contains(selected) ? selected : Rows.Count > 1 ? Rows[1] : null;
        }

        OnPropertyChanged(nameof(HasGoals));
        OnPropertyChanged(nameof(NoGoals));
    }

    private void ShowPlan(GoalPlan plan)
    {
        _loaded = true;
        Dictionary<Guid, GoalNeeds> byGoal = plan.Goals.ToDictionary(n => n.Goal.Id);
        foreach (GoalRow row in Rows.Where(r => r.IsGoal))
        {
            row.Needs = byGoal.GetValueOrDefault(row.Id);
        }

        _all.Needs = Merge(plan.Goals);
        BuyCopper = plan.BuyCopper;
        CoinCopper = _holdings.Wallet.GetValueOrDefault(AccountValuation.CoinCurrencyId);
        OnPropertyChanged(nameof(ShortCopper));
        OnPropertyChanged(nameof(IsShort));
        OnPropertyChanged(nameof(CoinCovers));
        OnPropertyChanged(nameof(NoGoals));
        ShowNeeds();
    }

    // Every goal's needs as one: lines summed by item, currencies by currency.
    private static GoalNeeds Merge(IReadOnlyList<GoalNeeds> goals)
    {
        List<GoalLine> Sum(Func<GoalNeeds, IReadOnlyList<GoalLine>> lines) => goals
            .SelectMany(lines)
            .GroupBy(line => line.ItemId)
            .Select(g => g.First() with
            {
                Count = g.Sum(l => l.Count),
                Copper = g.Sum(l => l.Copper),
                Note = g.Select(l => l.Note).OfType<string>().Distinct().ToList() is { Count: > 0 } notes
                    ? string.Join("; ", notes)
                    : null,
            })
            .OrderByDescending(line => line.Copper)
            .ThenBy(line => line.Name)
            .ToList();

        List<GoalCurrency> currencies = goals
            .SelectMany(g => g.Currencies)
            .GroupBy(c => (c.CurrencyId, c.ItemId, c.Name))
            .Select(g => g.First() with { Needed = g.Sum(c => c.Needed), Have = g.Sum(c => c.Have) })
            .OrderByDescending(c => c.Missing)
            .ThenBy(c => c.Name)
            .ToList();
        return new GoalNeeds(new Goal(), Sum(g => g.Covered), Sum(g => g.ToBuy), Sum(g => g.FromVendors), currencies,
            Sum(g => g.CantBuy), goals.Sum(g => g.Value), goals.Sum(g => g.CoveredValue));
    }

    private void ShowNeeds()
    {
        var rows = new List<object>();
        if (Selected?.Needs is { } needs)
        {
            void Section(string title, string description, IReadOnlyList<GoalLine> lines)
            {
                if (lines.Count > 0)
                {
                    rows.Add(new GoalHeading(title, description, lines.Sum(l => l.Copper)));
                    rows.AddRange(lines.Select(l => new GoalLineRow(l)));
                }
            }

            Section("To buy", "On the trading post, at the lowest listings.", needs.ToBuy);
            Section("From vendors", "Cheaper from a vendor, or only sold there.", needs.FromVendors);
            if (needs.Currencies.Count > 0)
            {
                rows.Add(new GoalHeading("Currencies", "What vendors take for those, against your wallet.", 0));
                rows.AddRange(needs.Currencies.Select(c => new GoalCurrencyRow(c)));
            }

            Section("Can't be bought", "Account-bound or earned by playing: these you have to get yourself.",
                needs.CantBuy);
            Section("Covered", "Set aside from what you hold.", needs.Covered);
        }

        Details = rows;
        OnPropertyChanged(nameof(Details));
    }
}
