using Gw2Gizmos.Data.EntityFramework.Entities.Accounts;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.Data.EntityFramework.Entities.Currencies;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Data.EntityFramework.Entities.Materials;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework;

public class Gw2GizmosDbContext : DbContext
{
    public Gw2GizmosDbContext(DbContextOptions<Gw2GizmosDbContext> dbContextOptions)
        : base(dbContextOptions) { }

    // Items
    public DbSet<Item> Items { get; set; }
    public DbSet<Armor> Armors { get; set; }
    public DbSet<BackItem> BackItems { get; set; }
    public DbSet<Bag> Bags { get; set; }
    public DbSet<Consumable> Consumables { get; set; }
    public DbSet<Container> Containers { get; set; }
    public DbSet<Gathering> Gatherings { get; set; }
    public DbSet<Gizmo> Gizmos { get; set; }
    public DbSet<MiniPet> MiniPets { get; set; }
    public DbSet<Tool> Tools { get; set; }
    public DbSet<Trinket> Trinkets { get; set; }
    public DbSet<UpgradeComponent> UpgradeComponents { get; set; }
    public DbSet<Weapon> Weapons { get; set; }

    // Details
    public DbSet<ArmorDetails> ArmorDetails { get; set; }
    public DbSet<BackItemDetails> BackItemDetails { get; set; }
    public DbSet<BagDetails> BagDetails { get; set; }
    public DbSet<ConsumableDetails> ConsumableDetails { get; set; }
    public DbSet<ContainerDetails> ContainerDetails { get; set; }
    public DbSet<GatheringDetails> GatheringDetails { get; set; }
    public DbSet<MiniPetDetails> MiniPetDetails { get; set; }
    public DbSet<ToolDetails> ToolDetails { get; set; }
    public DbSet<TrinketDetails> TrinketDetails { get; set; }
    public DbSet<UpgradeComponentDetails> UpgradeComponentDetails { get; set; }
    public DbSet<WeaponDetails> WeaponDetails { get; set; }

    // Precomputed craft-cost cache (cheapest fully-priced craft cost per craftable item) for the Items grid;
    // live prices/profit/margin are derived from PriceSnapshots at read time, not stored here.
    public DbSet<ItemCraftCost> ItemCraftCosts { get; set; }

    // Append-only trading-post price history (one row per item per market refresh; downsampled over time).
    public DbSet<PriceSnapshot> PriceSnapshots { get; set; }

    // Currencies
    public DbSet<Currency> Currencies { get; set; }

    // Material-storage categories (master data) + their ordered item membership, for the Account screen grid.
    public DbSet<MaterialCategory> MaterialCategories { get; set; }
    public DbSet<MaterialCategoryItem> MaterialCategoryItems { get; set; }

    // Recipes
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    // Authenticated account data (keyed by account id). Item holdings (material storage, bank, shared inventory,
    // character bags) are one append-on-change log discriminated by location; the wallet is its own log
    // (currencies aren't items). AccountContainerSlots / CharacterItemSlots are the current slot-by-slot grids
    // (bank + shared inventory, and per-character bags), replaced each sync.
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<AccountWalletObservation> AccountWalletObservations { get; set; }
    public DbSet<AccountItemObservation> AccountItemObservations { get; set; }
    public DbSet<AccountContainerSlot> AccountContainerSlots { get; set; }
    public DbSet<CharacterItemSlot> CharacterItemSlots { get; set; }
}
