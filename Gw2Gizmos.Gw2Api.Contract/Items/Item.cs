using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.Items;

// [JsonPolymorphic(
//      TypeDiscriminatorPropertyName = "Type",
//      UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType,
//      IgnoreUnrecognizedTypeDiscriminators = true
// )]
// [JsonDerivedType(typeof(Item<object>))]
// [JsonDerivedType(typeof(Armor), ItemTypes.Armor)]
public class Item
{
     public ItemType Type { get; set; }
     public int Id { get; set; }
     public string Name { get; set; }
     public string ChatLink { get; set; }

     public ItemRarity Rarity { get; set; }
     public int Level { get; set; }
     public ItemGameType[] GameTypes { get; set; } = Array.Empty<ItemGameType>();
     public ItemFlag[] Flags { get; set; } = Array.Empty<ItemFlag>();
     public ItemRestriction[] Restrictions { get; set; } = Array.Empty<ItemRestriction>();

     public string? Description { get; set; }
     public int? DefaultSkint { get; set; }
     public string? RenderUrl { get; set; }
}

public record Item<TDetails>
{
     public required TDetails Details { get; set; }
}