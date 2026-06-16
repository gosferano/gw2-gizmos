namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The <c>uiState</c> bitmask from MumbleLink's context, exposed as named flag accessors. Carries the raw
/// <see cref="Value"/> with implicit conversions so unknown future bits are preserved.
/// </summary>
public readonly struct UiState : IEquatable<UiState>
{
    public uint Value { get; }

    private UiState(uint value)
    {
        Value = value;
    }

    public bool IsMapOpen => (Value & (1u << 0)) != 0;
    public bool IsCompassTopRight => (Value & (1u << 1)) != 0;
    public bool DoesCompassHaveRotationEnabled => (Value & (1u << 2)) != 0;
    public bool GameHasFocus => (Value & (1u << 3)) != 0;
    public bool IsInCompetitiveGameMode => (Value & (1u << 4)) != 0;
    public bool TextboxHasFocus => (Value & (1u << 5)) != 0;
    public bool IsInCombat => (Value & (1u << 6)) != 0;

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator UiState(uint value) => new(value);

    public static implicit operator uint(UiState value) => value.Value;

    public bool Equals(UiState other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UiState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(UiState left, UiState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UiState left, UiState right)
    {
        return !left.Equals(right);
    }
}
