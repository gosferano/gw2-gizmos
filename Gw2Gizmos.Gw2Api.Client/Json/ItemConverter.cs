using System.Text.Json;
using System.Text.Json.Serialization;
using Gw2Gizmos.Gw2Api.Contract.Items;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public class ItemJsonConverter : JsonConverter<Item>
{
    private const string TypePropertyName = "type";

    private static readonly Dictionary<string, Type> TypeMap =
        new()
        {
            { ItemType.Armor, typeof(Armor) },
            { ItemType.Bag, typeof(Bag) },
            { ItemType.Back, typeof(BackItem) },
            { ItemType.Consumable, typeof(Consumable) },
            { ItemType.Container, typeof(Container) },
            { ItemType.Gathering, typeof(Gathering) },
            { ItemType.Gizmo, typeof(Gizmo) },
            { ItemType.MiniPet, typeof(MiniPet) },
            { ItemType.Tool, typeof(Tool) },
            { ItemType.Trinket, typeof(Trinket) },
            { ItemType.UpgradeComponent, typeof(UpgradeComponent) },
            { ItemType.Weapon, typeof(Weapon) },
        };

    public override Item Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of the object");
        }

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);

        if (!doc.RootElement.TryGetProperty(TypePropertyName, out JsonElement typeProperty))
        {
            throw new JsonException($"Expected '{TypePropertyName}' property");
        }

        string? itemType = typeProperty.GetString();

        if (string.IsNullOrWhiteSpace(itemType))
        {
            throw new JsonException($"Expected '{TypePropertyName}' property to to not be null or whitespace");
        }

        if (!TypeMap.TryGetValue(itemType, out Type? type))
        {
            throw new JsonException($"Unsupported type '{itemType}'");
        }

        return (doc.RootElement.Deserialize(type, options) as Item)!;
    }

    public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
