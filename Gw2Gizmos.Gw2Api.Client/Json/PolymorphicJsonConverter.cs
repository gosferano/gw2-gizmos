using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Client.Json;

/// <remarks>
/// Only supports polymorphism based on single string property.
/// </remarks>
public abstract class PolymorphicJsonConverter<T> : JsonConverter<T>
    where T : class
{
    protected abstract string TypePropertyName { get; }

    protected abstract Dictionary<string, Type> TypeMap { get; }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        return (doc.RootElement.Deserialize(type, options) as T)!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
