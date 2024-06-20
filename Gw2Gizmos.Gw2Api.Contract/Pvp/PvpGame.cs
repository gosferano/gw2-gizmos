namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public class PvpGame
{
    public string Id { get; set; }
    public int MapId { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset Ended { get; set; }
    public PvpResult Result { get; set; }
    public PvpTeam Team { get; set; }
    public ProfessionName Profession { get; set; }
    public PvpTeamScores Scores { get; set; }
    public PvpRatingType RatingType { get; set; }
    public int RatingChange { get; set; }
    public string? Season { get; set; }
}

public readonly struct PvpTeam
{
    public static readonly PvpTeam Blue = new("Blue");
    public static readonly PvpTeam Red = new("Red");

    public string Value { get; }

    private PvpTeam(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpTeam(string value) => new(value);

    public static implicit operator string(PvpTeam value) => value.Value;

    public bool Equals(PvpTeam other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpTeam other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpTeam left, PvpTeam right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpTeam left, PvpTeam right)
    {
        return !left.Equals(right);
    }
}

public readonly struct PvpResult : IEquatable<PvpResult>
{
    public static readonly PvpResult Defeat = new PvpResult("Defeat");
    public static readonly PvpResult Victory = new PvpResult("Victory");

    public string Value { get; }

    private PvpResult(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpResult(string value) => new(value);

    public static implicit operator string(PvpResult value) => value.Value;

    public bool Equals(PvpResult other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpResult left, PvpResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpResult left, PvpResult right)
    {
        return !left.Equals(right);
    }
}
