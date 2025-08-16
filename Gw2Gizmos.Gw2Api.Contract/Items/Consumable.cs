namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class Consumable : Item
{
    public ConsumableDetails Details { get; set; }
}

public class ConsumableDetails
{
    public ConsumableType Type { get; set; }
    public string? Description { get; set; }
    public int? DurationMs { get; set; }
    public ConsumableUnlockType? UnlockType { get; set; }
    public int? ColorId { get; set; }
    public int? RecipeId { get; set; }
    public int[]? ExtraRecipeIds { get; set; }
    public int? GuildUpgradeId { get; set; }
    public int? ApplyCount { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public int[]? Skins { get; set; }
}
