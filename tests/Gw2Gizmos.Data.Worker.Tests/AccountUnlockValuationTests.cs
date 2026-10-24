using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>
/// Values the account's unlocks at the cheapest item with a value that grants each, its equipment (what's slotted in
/// legendaries included, the legendaries themselves not) and its legendary armory.
/// </summary>
public sealed class AccountUnlockValuationTests : IDisposable
{
    private const string AccountId = "acc";

    private readonly WorkerDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private void Seed(Action<Gw2GizmosDbContext> seed)
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        seed(db);
        db.SaveChanges();
    }

    private static Item Item(int id, int? defaultSkin = null) =>
        new()
        {
            Id = id, ChatLink = "", Name = $"Item {id}", Type = "Weapon", Rarity = "Exotic", DefaultSkin = defaultSkin,
        };

    private static Consumable Unlocker(int id, string unlockType, int? colorId = null, int? recipeId = null) =>
        new()
        {
            Id = id, ChatLink = "", Name = $"Item {id}", Type = "Consumable", Rarity = "Basic",
            Details = new ConsumableDetails
            {
                ItemId = id, Type = "Unlock", UnlockType = unlockType, ColorId = colorId, RecipeId = recipeId,
            },
        };

    // Sells instantly for 85 of every 100 of its buy order.
    private static PriceSnapshot Buy(int itemId, int buy) =>
        new() { ItemId = itemId, TimestampUtc = DateTimeOffset.UtcNow, Buy = buy, Sell = buy + 1 };

    private static AccountUnlock Unlocked(string kind, int id) =>
        new() { AccountId = AccountId, Kind = kind, UnlockId = id };

    private Dictionary<string, long> ByKind() => AccountUnlockValuation.ByKind(_fixture.NewContext(), AccountId);

    [Fact]
    public void A_skin_is_worth_its_cheapest_item_with_a_value()
    {
        // Skin 10 comes on three weapons: one buys for 1000, one for 200, and one has no value (gem store, say).
        Seed(db =>
        {
            db.Items.AddRange(Item(1, defaultSkin: 10), Item(2, defaultSkin: 10), Item(3, defaultSkin: 10));
            db.PriceSnapshots.AddRange(Buy(1, 1000), Buy(2, 200));
            db.AccountUnlocks.Add(Unlocked(AccountUnlockKind.Skin, 10));
        });

        Assert.Equal(170, ByKind()[AccountUnlockKind.Skin]);
    }

    [Fact]
    public void An_unlock_no_item_with_a_value_grants_is_worth_nothing()
    {
        Seed(db =>
        {
            db.Items.Add(Item(1, defaultSkin: 10));
            db.AccountUnlocks.AddRange(Unlocked(AccountUnlockKind.Skin, 10), Unlocked(AccountUnlockKind.Skin, 11));
        });

        Assert.Equal(0, ByKind()[AccountUnlockKind.Skin]);
    }

    [Fact]
    public void Dyes_minis_recipes_and_catalog_unlocks_are_valued_by_their_items()
    {
        Seed(db =>
        {
            db.Items.AddRange(Unlocker(20, "Dye", colorId: 5), Unlocker(21, "CraftingRecipe", recipeId: 7),
                Item(22), Item(23));
            db.Minis.Add(new Mini { Id = 3, Name = "Mini", ItemId = 22 });
            db.UnlockItems.Add(new UnlockItem { Kind = AccountUnlockKind.Outfit, UnlockId = 4, ItemId = 23 });
            db.PriceSnapshots.AddRange(Buy(20, 100), Buy(21, 200), Buy(22, 300), Buy(23, 400));
            db.AccountUnlocks.AddRange(Unlocked(AccountUnlockKind.Dye, 5), Unlocked(AccountUnlockKind.Mini, 3),
                Unlocked(AccountUnlockKind.Outfit, 4));
            db.AccountRecipes.Add(new AccountRecipe { AccountId = AccountId, RecipeId = 7 });
        });

        Dictionary<string, long> values = ByKind();

        Assert.Equal((85, 255, 340, 170),
            (values[AccountUnlockKind.Dye], values[AccountUnlockKind.Mini], values[AccountUnlockKind.Outfit],
                values[AccountUnlockValuation.Recipe]));
    }

    [Fact]
    public void An_unlock_whose_item_is_held_equipped_or_in_the_armory_is_already_counted()
    {
        // Skins 10–13 each come on one weapon buying for 1000: the first is in the bank, the second equipped, the
        // third in the legendary armory; only the fourth's is counted as an unlock.
        Seed(db =>
        {
            db.Items.AddRange(Item(1, defaultSkin: 10), Item(2, defaultSkin: 11), Item(3, defaultSkin: 12),
                Item(4, defaultSkin: 13));
            db.PriceSnapshots.AddRange(Buy(1, 1000), Buy(2, 1000), Buy(3, 1000), Buy(4, 1000));
            db.AccountUnlocks.AddRange(Unlocked(AccountUnlockKind.Skin, 10), Unlocked(AccountUnlockKind.Skin, 11),
                Unlocked(AccountUnlockKind.Skin, 12), Unlocked(AccountUnlockKind.Skin, 13));
            db.AccountItemObservations.Add(new AccountItemObservation
            {
                AccountId = AccountId, Container = AccountContainer.Bank, ItemId = 1, Count = 1,
                ObservedAtUtc = DateTimeOffset.UtcNow,
            });
            db.CharacterEquippedItems.Add(Equipped(0, 2, EquippedPart.Item, "Equipped"));
            db.AccountLegendaryArmory.Add(new AccountLegendaryItem { AccountId = AccountId, ItemId = 3, Count = 1 });
        });

        Assert.Equal(850, ByKind()[AccountUnlockKind.Skin]);
    }

    [Fact]
    public void Equipment_counts_whats_slotted_in_legendaries_but_not_the_legendaries()
    {
        Seed(db =>
        {
            db.Items.AddRange(Item(1), Item(2), Item(3));
            db.PriceSnapshots.AddRange(Buy(1, 1000), Buy(2, 100), Buy(3, 50_000));
            db.CharacterEquippedItems.AddRange(
                Equipped(0, 1, EquippedPart.Item, "Equipped"),
                Equipped(1, 3, EquippedPart.Item, "EquippedFromLegendaryArmory"),
                Equipped(2, 2, EquippedPart.Infusion, "EquippedFromLegendaryArmory"));
            db.AccountLegendaryArmory.Add(new AccountLegendaryItem { AccountId = AccountId, ItemId = 3, Count = 2 });
        });

        using Gw2GizmosDbContext db = _fixture.NewContext();
        Assert.Equal(850 + 85, AccountValuation.EquipmentValue(db, AccountId));
        Assert.Equal(2 * 42_500, AccountValuation.LegendaryArmoryValue(db, AccountId));
    }

    private static CharacterEquippedItem Equipped(int index, int itemId, string part, string location) =>
        new()
        {
            AccountId = AccountId, CharacterName = "Alice", Index = index, ItemId = itemId, Part = part,
            Location = location,
        };
}
