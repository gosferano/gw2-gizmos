namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class Gizmo : Item
{
    public GizmoDetails Details { get; set; } = null!;
}

public sealed class GizmoDetails
{
    public GizmoType Type { get; set; }

    /// <summary>The guild upgrade the gizmo unlocks, if any (e.g. a guild hall decoration).</summary>
    public int? GuildUpgradeId { get; set; }

    /// <summary>The vendors the gizmo opens, if any.</summary>
    public int[] VendorIds { get; set; } = Array.Empty<int>();
}
