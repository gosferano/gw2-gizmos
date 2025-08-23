namespace Gw2Gizmos.Gw2Api.Contract.V2.Recipes;

public readonly struct RecipeFlag : IEquatable<RecipeFlag>
{
    public static readonly RecipeFlag AutoLearned = new("AutoLearned");
    public static readonly RecipeFlag LearnedFromItem = new("LearnedFromItem");

    public string Value { get; }

    private RecipeFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator RecipeFlag(string value) => new(value);

    public static implicit operator string(RecipeFlag value) => value.Value;

    public bool Equals(RecipeFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is RecipeFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(RecipeFlag left, RecipeFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RecipeFlag left, RecipeFlag right)
    {
        return !left.Equals(right);
    }
}
