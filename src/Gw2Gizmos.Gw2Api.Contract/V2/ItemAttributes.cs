using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.V2;

/// <summary>
/// Attribute bonuses keyed by attribute (a PvP amulet's, a character equipment piece's stats). The API spells the keys
/// in PascalCase ("ConditionDamage"), which the contract's snake_case naming would never match for a two-word
/// attribute, so each is named explicitly.
/// </summary>
public sealed class ItemAttributes
{
    [JsonPropertyName("AgonyResistance")]
    public decimal? AgonyResistance { get; set; }

    [JsonPropertyName("BoonDuration")]
    public decimal? BoonDuration { get; set; }

    [JsonPropertyName("ConditionDamage")]
    public decimal? ConditionDamage { get; set; }

    [JsonPropertyName("ConditionDuration")]
    public decimal? ConditionDuration { get; set; }

    [JsonPropertyName("CritDamage")]
    public decimal? CritDamage { get; set; }

    [JsonPropertyName("Healing")]
    public decimal? Healing { get; set; }

    [JsonPropertyName("Power")]
    public decimal? Power { get; set; }

    [JsonPropertyName("Precision")]
    public decimal? Precision { get; set; }

    [JsonPropertyName("Toughness")]
    public decimal? Toughness { get; set; }

    [JsonPropertyName("Vitality")]
    public decimal? Vitality { get; set; }
}
