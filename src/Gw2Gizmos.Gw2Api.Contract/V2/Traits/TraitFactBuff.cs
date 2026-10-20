namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public class TraitFactBuff : TraitFact
{
    public int? Duration { get; set; }
    /// <summary>
    /// The buff's name, e.g. "Might". A prefixed buff (<see cref="TraitFactPrefixedBuff"/>) can leave it out and name
    /// only its prefix, e.g. Elemental Synergy's facts, which list attunements and no buff.
    /// </summary>
    public string? Status { get; set; }
    public string? Description { get; set; }
    public int? ApplyCount { get; set; }
}
