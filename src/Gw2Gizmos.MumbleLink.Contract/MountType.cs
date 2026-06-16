using System.Collections.Generic;

namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The mount the character is currently riding, as the <c>mountIndex</c> byte in MumbleLink's context
/// (<see cref="None"/> when on foot). Unknown ids flow through via the implicit conversion as their raw value.
/// </summary>
public readonly struct MountType : IEquatable<MountType>
{
    public static readonly MountType None = new(0);
    public static readonly MountType Jackal = new(1);
    public static readonly MountType Griffon = new(2);
    public static readonly MountType Springer = new(3);
    public static readonly MountType Skimmer = new(4);
    public static readonly MountType Raptor = new(5);
    public static readonly MountType RollerBeetle = new(6);
    public static readonly MountType Warclaw = new(7);
    public static readonly MountType Skyscale = new(8);
    public static readonly MountType Skiff = new(9);
    public static readonly MountType SiegeTurtle = new(10);

    /// <summary>Every mount index, in id order (index 0 is "on foot").</summary>
    public static readonly IReadOnlyList<MountType> All = new[]
    {
        None, Jackal, Griffon, Springer, Skimmer, Raptor,
        RollerBeetle, Warclaw, Skyscale, Skiff, SiegeTurtle,
    };

    public int Value { get; }

    private MountType(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator MountType(int value) => new(value);

    public static implicit operator int(MountType value) => value.Value;

    public bool Equals(MountType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MountType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(MountType left, MountType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MountType left, MountType right)
    {
        return !left.Equals(right);
    }
}
