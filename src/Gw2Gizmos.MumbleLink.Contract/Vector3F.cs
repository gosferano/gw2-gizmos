namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>A 3-component single-precision vector — one of MumbleLink's <c>float[3]</c> fields (position/front/top).</summary>
public readonly struct Vector3F : IEquatable<Vector3F>
{
    public static readonly Vector3F Zero = new(0f, 0f, 0f);

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3F(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }

    public bool Equals(Vector3F other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3F other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Vector3F left, Vector3F right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3F left, Vector3F right)
    {
        return !left.Equals(right);
    }
}
