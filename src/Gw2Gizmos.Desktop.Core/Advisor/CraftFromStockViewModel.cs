using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A group's heading in the Craft from stock list.</summary>
public sealed record StockCraftHeading(string Title);

/// <summary>A craft's row on the Craft from stock page.</summary>
public sealed class StockCraftRow
{
    private readonly StockCraft _craft;
    private readonly IReadOnlyDictionary<int, string> _names;

    public StockCraftRow(StockCraft craft, IReadOnlyDictionary<int, string> names)
    {
        _craft = craft;
        _names = names;
    }

    public int ItemId => _craft.Recipe.OutputItemId;

    public string Name => NameOf(ItemId);

    /// <summary>E.g. "Jeweler 400 · makes 10 · needs its recipe sheet".</summary>
    public string Details
    {
        get
        {
            IReadOnlyList<string> disciplines = _craft.Recipe.Disciplines;
            string who = disciplines.Count switch
            {
                0 => "",
                1 => disciplines[0],
                _ => $"{string.Join(", ", disciplines.Take(disciplines.Count - 1))} or {disciplines[^1]}",
            };
            return string.Join(" · ", new[]
            {
                $"{who} {_craft.Recipe.MinRating}".Trim(),
                _craft.Recipe.OutputCount > 1 ? $"makes {_craft.Recipe.OutputCount}" : null,
                _craft.SoldPerWeek is { } sold ? $"sells ~{sold:N0} a week" : null,
                _craft.Known == true ? null
                    : _craft.Recipe.LearnedFromItem ? "needs its recipe sheet"
                    : _craft.Known == false && !_craft.Recipe.AutoLearned ? "needs discovering" : null,
                _craft.CharactersCanCraft ? null : "no character has the level",
            }.Where(part => !string.IsNullOrEmpty(part)));
        }
    }

    /// <summary>E.g. "Uses 10 Gold Ingot, 1 Lump of Mithrillium (at craft cost)".</summary>
    public string UsesText => "Uses " + string.Join(", ", _craft.Uses.Select(u =>
        $"{u.Count:N0} {NameOf(u.ItemId)}"
        + (u.AtCraftCost ? " (at craft cost)" : u.Copper == 0 ? " (no sell value)" : "")));

    /// <summary>E.g. "Buy 10 Orichalcum Setting"; empty when everything is in stock.</summary>
    public string MissingText => _craft.Missing.Count == 0
        ? ""
        : "Buy " + string.Join(", ", _craft.Missing.Select(m => $"{m.Count:N0} {NameOf(m.ItemId)}"));

    public bool HasMissing => _craft.Missing.Count > 0;

    /// <summary>How the profit of one craft is made up.</summary>
    public string Breakdown => string.Join(Environment.NewLine, new[]
    {
        $"Sells for {Coin.Format(_craft.OutputCopper)} at the highest buy order, after fees",
        $"Your materials would sell for {Coin.Format(_craft.UsesCopper)}",
        HasMissing ? $"The rest costs {Coin.Format(_craft.MissingCopper)} at the lowest listings" : null,
        _craft.ToMake < _craft.CanMake
            ? $"Stock makes {Number(_craft.CanMake)}, but only ~{Number(_craft.SoldPerWeek ?? 0)} sold a week"
            : null,
    }.Where(line => line is not null));

    public long ProfitCopper => _craft.ProfitCopper;

    public long StockProfitCopper => _craft.StockProfitCopper;

    /// <summary>E.g. "each × 12", or "each × 12 of 40" when the market takes fewer than stock allows.</summary>
    public string CanMakeText => _craft.ToMake < _craft.CanMake
        ? $"each × {Number(_craft.ToMake)} of {Number(_craft.CanMake)}"
        : $"each × {Number(_craft.CanMake)}";

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);

    private string NameOf(int itemId) => _names.GetValueOrDefault(itemId) ?? $"Item {itemId}";
}

/// <summary>
/// Backs the Advisor › Craft from stock page: what's worth crafting from the materials the account holds, rather than
/// selling them (<see cref="CraftFromStock"/>). Crafts that need nothing bought come first, by what crafting as many
/// as stock allows and a week's sales take makes; then those short of a few materials, by what one makes. Refreshes
/// after syncs and price polls.
/// </summary>
public sealed class CraftFromStockViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private StockCrafts _crafts = StockCrafts.Empty;
    private bool _onlyCharactersCanCraft = true;
    private bool _onlyKnown = true;
    private bool _loaded;

    public CraftFromStockViewModel(AccountReader reader, SelectedAccountService selected, DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = string.IsNullOrEmpty(selected.AccountId) ? null : selected.AccountId;
        HasAccount = _accountId is not null;
        Breadcrumbs = AdvisorBreadcrumbs.Section("Craft from stock");
        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(changes =>
        {
            // Holdings move with syncs, prices with each poll.
            if ((changes & (DataChanges.AccountSync | DataChanges.Holdings | DataChanges.Catalog)) != 0)
            {
                _ = LoadAsync();
            }
        });
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    public bool HasAccount { get; }

    /// <summary>Whether the characters' crafting levels are known, so crafts can be limited to theirs.</summary>
    public bool HasRatings => _crafts.HasRatings;

    /// <summary>Leave out crafts no character has the discipline and level for.</summary>
    public bool OnlyCharactersCanCraft
    {
        get => _onlyCharactersCanCraft;
        set
        {
            if (SetProperty(ref _onlyCharactersCanCraft, value))
            {
                Apply();
            }
        }
    }

    /// <summary>Whether the account's learned recipes are synced, so crafts can be limited to those it knows.</summary>
    public bool HasLearned => _crafts.HasLearned;

    /// <summary>Leave out recipes the account hasn't learned.</summary>
    public bool OnlyKnown
    {
        get => _onlyKnown;
        set
        {
            if (SetProperty(ref _onlyKnown, value))
            {
                Apply();
            }
        }
    }

    /// <summary>
    /// One list, so it can be virtualized (hundreds of rows): crafts made entirely from stock, by what crafting as
    /// many as are worth making makes, then those short of a few materials, by what one makes; each group under a
    /// <see cref="StockCraftHeading"/>.
    /// </summary>
    public IReadOnlyList<object> Rows { get; private set; } = [];

    public bool NoCrafts => _loaded && Rows.Count == 0;

    /// <summary>
    /// The crafts to show: those the characters can make and those the account knows, when each is asked for and
    /// known.
    /// </summary>
    public static IEnumerable<StockCraft> Shown(StockCrafts crafts, bool onlyCharactersCanCraft, bool onlyKnown) =>
        crafts.Crafts.Where(c => (!onlyCharactersCanCraft || !crafts.HasRatings || c.CharactersCanCraft)
            && (!onlyKnown || c.Known != false));

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            _crafts = await Task.Run(() => _reader.GetStockCrafts(accountId));
            _loaded = true;
            OnPropertyChanged(nameof(HasRatings));
            OnPropertyChanged(nameof(HasLearned));
            Apply();
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }

    private void Apply()
    {
        List<StockCraft> shown = Shown(_crafts, _onlyCharactersCanCraft, _onlyKnown).ToList();
        List<StockCraft> ready = shown.Where(c => c.Missing.Count == 0)
            .OrderByDescending(c => c.StockProfitCopper)
            .ToList();
        List<StockCraft> buying = shown.Where(c => c.Missing.Count > 0)
            .OrderByDescending(c => c.ProfitCopper)
            .ToList();

        var rows = new List<object>();
        foreach ((string title, List<StockCraft> crafts) in new[]
                 { ("From stock", ready), ("Buying a few materials", buying) })
        {
            if (crafts.Count > 0)
            {
                rows.Add(new StockCraftHeading(title));
                rows.AddRange(crafts.Select(c => new StockCraftRow(c, _crafts.Names)));
            }
        }

        Rows = rows;
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(NoCrafts));
    }
}
