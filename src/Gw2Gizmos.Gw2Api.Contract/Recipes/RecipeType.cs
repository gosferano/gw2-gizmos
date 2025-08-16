namespace Gw2Gizmos.Gw2Api.Contract.Recipes;

public readonly struct RecipeType : IEquatable<RecipeType>
{
    // Weapon types
    public static readonly RecipeType Axe = new("Axe");
    public static readonly RecipeType Dagger = new("Dagger");
    public static readonly RecipeType Focus = new("Focus");
    public static readonly RecipeType Greatsword = new("Greatsword");
    public static readonly RecipeType Hammer = new("Hammer");
    public static readonly RecipeType Harpoon = new("Harpoon");
    public static readonly RecipeType LongBow = new("LongBow");
    public static readonly RecipeType Mace = new("Mace");
    public static readonly RecipeType Pistol = new("Pistol");
    public static readonly RecipeType Rifle = new("Rifle");
    public static readonly RecipeType Scepter = new("Scepter");
    public static readonly RecipeType Shield = new("Shield");
    public static readonly RecipeType ShortBow = new("ShortBow");
    public static readonly RecipeType Speargun = new("Speargun");
    public static readonly RecipeType Staff = new("Staff");
    public static readonly RecipeType Sword = new("Sword");
    public static readonly RecipeType Torch = new("Torch");
    public static readonly RecipeType Trident = new("Trident");
    public static readonly RecipeType Warhorn = new("Warhorn");

    // Armor types
    public static readonly RecipeType Boots = new("Boots");
    public static readonly RecipeType Coat = new("Coat");
    public static readonly RecipeType Gloves = new("Gloves");
    public static readonly RecipeType Helm = new("Helm");
    public static readonly RecipeType Leggings = new("Leggings");
    public static readonly RecipeType Shoulders = new("Shoulders");

    // Trinket types
    public static readonly RecipeType Amulet = new("Amulet");
    public static readonly RecipeType Earring = new("Earring");
    public static readonly RecipeType Ring = new("Ring");

    // Food types
    public static readonly RecipeType Dessert = new("Dessert");
    public static readonly RecipeType Feast = new("Feast");
    public static readonly RecipeType IngredientCooking = new("IngredientCooking");
    public static readonly RecipeType Meal = new("Meal");
    public static readonly RecipeType Seasoning = new("Seasoning");
    public static readonly RecipeType Snack = new("Snack");
    public static readonly RecipeType Soup = new("Soup");
    public static readonly RecipeType Food = new("Food");

    // Crafting component types
    public static readonly RecipeType Component = new("Component");
    public static readonly RecipeType Inscription = new("Inscription");
    public static readonly RecipeType Insignia = new("Insignia");
    public static readonly RecipeType LegendaryComponent = new("LegendaryComponent");

    // Refinement types
    public static readonly RecipeType Refinement = new("Refinement");
    public static readonly RecipeType RefinementEctoplasm = new("RefinementEctoplasm");
    public static readonly RecipeType RefinementObsidian = new("RefinementObsidian");

    // Guild types
    public static readonly RecipeType GuildConsumable = new("GuildConsumable");
    public static readonly RecipeType GuildDecoration = new("GuildDecoration");
    public static readonly RecipeType GuildConsumableWvw = new("GuildConsumableWvw");

    // Other types
    public static readonly RecipeType Backpack = new("Backpack");
    public static readonly RecipeType Bag = new("Bag");
    public static readonly RecipeType Bulk = new("Bulk");
    public static readonly RecipeType Consumable = new("Consumable");
    public static readonly RecipeType Dye = new("Dye");
    public static readonly RecipeType Potion = new("Potion");
    public static readonly RecipeType UpgradeComponent = new("UpgradeComponent");

    public string Value { get; }

    private RecipeType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator RecipeType(string value) => new(value);

    public static implicit operator string(RecipeType value) => value.Value;

    public bool Equals(RecipeType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is RecipeType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(RecipeType left, RecipeType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RecipeType left, RecipeType right)
    {
        return !left.Equals(right);
    }
}
