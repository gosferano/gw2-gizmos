namespace Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

public sealed class HomesteadGlyph
{
    public string Id { get; set; } = null!;
    public int ItemId { get; set; }
    public HomesteadGlyphSlot Slot { get; set; }
}
