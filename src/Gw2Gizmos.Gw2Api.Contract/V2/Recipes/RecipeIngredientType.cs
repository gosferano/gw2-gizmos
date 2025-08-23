namespace Gw2Gizmos.Gw2Api.Contract.V2.Recipes;

public readonly struct RecipeIngredientType : IEquatable<RecipeIngredientType>
{
    public static readonly RecipeIngredientType Currency = new("Currency");
    public static readonly RecipeIngredientType Item = new("Item");
    public static readonly RecipeIngredientType GuildUpgrade = new("GuildUpgrade");

    public string Value { get; }

    private RecipeIngredientType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator RecipeIngredientType(string value) => new(value);

    public static implicit operator string(RecipeIngredientType value) => value.Value;

    public bool Equals(RecipeIngredientType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is RecipeIngredientType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(RecipeIngredientType left, RecipeIngredientType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RecipeIngredientType left, RecipeIngredientType right)
    {
        return !left.Equals(right);
    }
}
