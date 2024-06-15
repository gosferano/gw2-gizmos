namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct CharacterRace : IEquatable<CharacterRace>
{
    public static readonly CharacterRace Asura = new CharacterRace("Asura");
    public static readonly CharacterRace Charr = new CharacterRace("Charr");
    public static readonly CharacterRace Human = new CharacterRace("Human");
    public static readonly CharacterRace Norn = new CharacterRace("Norn");
    public static readonly CharacterRace Sylvari = new CharacterRace("Sylvari");

    public string Value { get; }

    private CharacterRace(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator CharacterRace(string value) => new(value);

    public static implicit operator string(CharacterRace value) => value.Value;

    public bool Equals(CharacterRace other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CharacterRace other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CharacterRace left, CharacterRace right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CharacterRace left, CharacterRace right)
    {
        return !left.Equals(right);
    }
}
