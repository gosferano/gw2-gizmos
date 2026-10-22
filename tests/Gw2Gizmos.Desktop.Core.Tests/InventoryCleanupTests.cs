using Gw2Gizmos.Desktop.Core;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// What to clean up in the bank, shared inventory and bags: partial stacks that fit in fewer slots, materials for
/// material storage (and beyond it), containers to open, and items worth selling that nothing uses.
/// </summary>
public sealed class InventoryCleanupTests
{
    private const int Essence = 1;
    private const int Ore = 2;
    private const int Chest = 3;
    private const int Sword = 4;
    private const int Gem = 5;

    private readonly List<HeldStack> _stacks = [];
    private readonly Dictionary<int, int> _storage = [];
    private readonly HashSet<int> _storageTakes = [Ore];
    private readonly Dictionary<int, CleanupItem> _items = new()
    {
        [Essence] = new("Consumable", false),
        [Ore] = new("CraftingMaterial", false),
        [Chest] = new("Container", false),
        [Sword] = new("Weapon", false),
        [Gem] = new("CraftingMaterial", false),
    };
    private readonly Dictionary<int, long> _values = [];
    private readonly HashSet<int> _used = [];
    private readonly Dictionary<int, IReadOnlyList<string>> _doneFor = [];
    private readonly Dictionary<int, CleanupUnlock> _unlocks = [];

    private List<CleanupSuggestion> Suggest(int capacity = 1_000) =>
        InventoryCleanup.Suggest(new CleanupInput(
            _stacks, _storage, _storageTakes, capacity, _items, _values, _used, _doneFor, _unlocks));

    [Fact]
    public void Partial_stacks_that_fit_in_fewer_slots_are_suggested_for_stacking()
    {
        _stacks.AddRange([new("Bank", Essence, 115), new("Gosferano", Essence, 65), new("Party Plants", Essence, 119)]);

        CleanupSuggestion stack = Assert.Single(Suggest(), s => s.Kind == CleanupKind.Stack);

        Assert.Equal(299, stack.Count);
        Assert.Equal((3, 2), stack.Slots);
        Assert.Equal(3, stack.Places.Count);
    }

    [Fact]
    public void Full_stacks_tools_with_charges_and_singles_of_equipment_arent_stacked()
    {
        _stacks.AddRange([new("Bank", Essence, 250), new("Bank", Essence, 250)]);
        _stacks.AddRange([new("Bank", Gem, 1, HasCharges: true), new("Gosferano", Gem, 1, HasCharges: true)]);
        _stacks.AddRange([new("Bank", Sword, 1), new("Gosferano", Sword, 1)]);

        Assert.DoesNotContain(Suggest(), s => s.Kind == CleanupKind.Stack);
    }

    [Fact]
    public void An_item_bound_to_characters_isnt_stacked_across_them()
    {
        _items[Essence] = new("Consumable", true);
        _stacks.AddRange([new("Gosferano", Essence, 5), new("Party Plants", Essence, 5)]);

        Assert.DoesNotContain(Suggest(), s => s.Kind == CleanupKind.Stack);
    }

    [Fact]
    public void Materials_go_to_storage_as_far_as_it_has_room_and_the_rest_is_overflow()
    {
        _storage[Ore] = 900;
        _values[Ore] = 10;
        _stacks.AddRange([new("Bank", Ore, 250), new("Gosferano", Ore, 50)]);

        List<CleanupSuggestion> suggestions = Suggest(capacity: 1_000);

        Assert.Equal(100, Assert.Single(suggestions, s => s.Kind == CleanupKind.Deposit).Count);
        CleanupSuggestion overflow = Assert.Single(suggestions, s => s.Kind == CleanupKind.Overflow);
        Assert.Equal((200, 2_000L), (overflow.Count, overflow.Copper));
        Assert.DoesNotContain(suggestions, s => s.Kind is CleanupKind.Stack or CleanupKind.Sell);
    }

    [Fact]
    public void A_container_is_suggested_for_opening_not_selling()
    {
        _values[Chest] = 500;
        _stacks.Add(new("Bank", Chest, 2));

        Assert.Equal(CleanupKind.Open, Assert.Single(Suggest()).Kind);
    }

    [Fact]
    public void An_item_worth_something_that_nothing_uses_is_suggested_for_selling()
    {
        _values[Sword] = 4_000;
        _stacks.Add(new("Erfindiel", Sword, 1));

        CleanupSuggestion sell = Assert.Single(Suggest());
        Assert.Equal((CleanupKind.Sell, 4_000L), (sell.Kind, sell.Copper));
    }

    [Fact]
    public void An_item_a_recipe_or_achievement_uses_or_worth_nothing_isnt_suggested_for_selling()
    {
        _values[Gem] = 100;
        _used.Add(Gem);
        _stacks.AddRange([new("Bank", Gem, 1), new("Bank", Sword, 1)]);

        Assert.Empty(Suggest());
    }

    [Theory]
    [InlineData(0, 250)]
    [InlineData(250, 250)]
    [InlineData(749, 750)]
    [InlineData(1_000, 1_000)]
    public void Storage_capacity_is_guessed_from_the_fullest_slot(int fullest, int capacity)
    {
        Assert.Equal(capacity, InventoryCleanup.GuessCapacity(new Dictionary<int, int> { [Ore] = fullest }));
    }

    [Fact]
    public void An_item_only_finished_achievements_asked_for_is_a_leftover_worth_something_or_not()
    {
        _doneFor[Gem] = ["Bloodstone Fen Insight"];
        _doneFor[Sword] = ["Lost Bandit Chests"];
        _values[Sword] = 300;
        _stacks.AddRange([new("Bank", Gem, 1), new("Bank", Sword, 1)]);

        List<CleanupSuggestion> leftovers = Suggest();

        Assert.All(leftovers, s => Assert.Equal(CleanupKind.Leftover, s.Kind));
        Assert.Equal([(Sword, 300L), (Gem, 0L)], leftovers.Select(s => (s.ItemId, s.Copper)).Order().ToList());
        Assert.Equal(["Bloodstone Fen Insight"], leftovers.Single(s => s.ItemId == Gem).DoneFor);
    }

    [Fact]
    public void An_item_still_used_elsewhere_isnt_a_leftover()
    {
        _doneFor[Gem] = ["Bloodstone Fen Insight"];
        _used.Add(Gem); // a recipe, or an unfinished achievement
        _stacks.Add(new("Bank", Gem, 1));

        Assert.Empty(Suggest());
    }

    [Fact]
    public void An_unlock_item_is_used_when_the_account_lacks_the_unlock_and_sold_when_it_has_it()
    {
        _unlocks[Gem] = new CleanupUnlock("dye", Have: false);
        _unlocks[Sword] = new CleanupUnlock("skin", Have: true);
        _values[Sword] = 300;
        _stacks.AddRange([new("Bank", Gem, 1), new("Bank", Sword, 1)]);

        List<CleanupSuggestion> suggestions = Suggest();

        CleanupSuggestion use = Assert.Single(suggestions, s => s.ItemId == Gem);
        Assert.Equal((CleanupKind.Consume, "dye"), (use.Kind, use.Unlocks));
        CleanupSuggestion sell = Assert.Single(suggestions, s => s.ItemId == Sword);
        Assert.Equal((CleanupKind.Unlocked, "skin", 300L), (sell.Kind, sell.Unlocks, sell.Copper));
    }
}
