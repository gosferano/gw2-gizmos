using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("Consumables")]
public class Consumable : Item
{
    public ConsumableDetails Details { get; set; }
}

[Table("ConsumableDetails")]
public class ConsumableDetails
{
    [Key, ForeignKey(nameof(Consumable))]
    public int ItemId { get; set; }
    public string Type { get; set; }
    public string? Description { get; set; }
    public int? DurationMs { get; set; }
    public string? UnlockType { get; set; }
    public int? ColorId { get; set; }
    public int? RecipeId { get; set; }
    public int? GuildUpgradeId { get; set; }
    public int? ApplyCount { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }

    // Navigation
    public Consumable Consumable { get; set; }
    public List<ConsumableExtraRecipe> ExtraRecipes { get; set; } = [];
    public List<ConsumableSkin> Skins { get; set; } = [];
}

[Table("ConsumableExtraRecipes")]
public class ConsumableExtraRecipe
{
    [Key]
    public int Id { get; set; }
    public int ConsumableId { get; set; }
    public int RecipeId { get; set; }

    [ForeignKey(nameof(ConsumableId))]
    public ConsumableDetails ConsumableDetails { get; set; }
}

[Table("ConsumableSkins")]
public class ConsumableSkin
{
    [Key]
    public int Id { get; set; }
    public int ConsumableId { get; set; }
    public int SkinId { get; set; }

    [ForeignKey(nameof(ConsumableId))]
    public ConsumableDetails ConsumableDetails { get; set; }
}
