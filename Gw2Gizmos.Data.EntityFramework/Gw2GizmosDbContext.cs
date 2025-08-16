using Gw2Gizmos.Data.EntityFramework.Entities.Items;
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
}
