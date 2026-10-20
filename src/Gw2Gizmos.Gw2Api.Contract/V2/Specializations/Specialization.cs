namespace Gw2Gizmos.Gw2Api.Contract.V2.Specializations;

public sealed class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Profession { get; set; } = null!;
    public bool Elite { get; set; }
    public int[] MinorTraits { get; set; } = Array.Empty<int>();
    public int[] MajorTraits { get; set; } = Array.Empty<int>();
    public string Icon { get; set; } = null!;
    public string Background { get; set; } = null!;

    /// <summary>The trait that unlocks an elite specialization's weapon (null for core specializations).</summary>
    public int? WeaponTrait { get; set; }

    /// <summary>The profession icon shown for this elite specialization (null for core specializations).</summary>
    public string? ProfessionIcon { get; set; }

    /// <summary>The large version of <see cref="ProfessionIcon"/>.</summary>
    public string? ProfessionIconBig { get; set; }
}
