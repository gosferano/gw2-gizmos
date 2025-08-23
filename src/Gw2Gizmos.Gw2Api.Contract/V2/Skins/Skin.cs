using Gw2Gizmos.Gw2Api.Contract.V2.Items;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

public class Skin
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SkinType Type { get; set; }
    public SkinFlag[] Flags { get; set; } = Array.Empty<SkinFlag>();
    public ItemRestriction[] Restrictions { get; set; } = Array.Empty<ItemRestriction>();
    public string Icon { get; set; }
    public ItemRarity Rarity { get; set; }
    public string Description { get; set; }
}
