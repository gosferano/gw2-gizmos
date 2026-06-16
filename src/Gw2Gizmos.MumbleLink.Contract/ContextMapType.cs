namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The kind of map the character is on, as the numeric <c>mapType</c> in MumbleLink's context. This is a different,
/// numeric enumeration from the GW2 API's string-valued map type, so it lives here as its own value-struct (the
/// contract takes no dependency on the API contract). Unknown ids flow through via the implicit conversion.
/// </summary>
public readonly struct ContextMapType : IEquatable<ContextMapType>
{
    public static readonly ContextMapType Redirect = new(0);
    public static readonly ContextMapType CharacterCreation = new(1);
    public static readonly ContextMapType Pvp = new(2);
    public static readonly ContextMapType Gvg = new(3);
    public static readonly ContextMapType Instance = new(4);
    public static readonly ContextMapType Public = new(5);
    public static readonly ContextMapType Tournament = new(6);
    public static readonly ContextMapType Tutorial = new(7);
    public static readonly ContextMapType UserTournament = new(8);
    public static readonly ContextMapType EternalBattlegrounds = new(9);
    public static readonly ContextMapType BlueBorderlands = new(10);
    public static readonly ContextMapType GreenBorderlands = new(11);
    public static readonly ContextMapType RedBorderlands = new(12);
    public static readonly ContextMapType FortunesVale = new(13);
    public static readonly ContextMapType ObsidianSanctum = new(14);
    public static readonly ContextMapType EdgeOfTheMists = new(15);
    public static readonly ContextMapType PublicMini = new(16);
    public static readonly ContextMapType BigBattle = new(17);
    public static readonly ContextMapType WvwLounge = new(18);

    public int Value { get; }

    private ContextMapType(int value)
    {
        Value = value;
    }

    /// <summary>True for the five WvW map types (Eternal Battlegrounds, the three borderlands, Edge of the Mists).</summary>
    public bool IsWorldVsWorld =>
        Value == EternalBattlegrounds.Value
        || Value == BlueBorderlands.Value
        || Value == GreenBorderlands.Value
        || Value == RedBorderlands.Value
        || Value == EdgeOfTheMists.Value;

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator ContextMapType(int value) => new(value);

    public static implicit operator int(ContextMapType value) => value.Value;

    public bool Equals(ContextMapType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ContextMapType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ContextMapType left, ContextMapType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContextMapType left, ContextMapType right)
    {
        return !left.Equals(right);
    }
}
