using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.Json;

public sealed class StringValueStructConverter<T> : JsonConverter<T>
    where T : struct
{
    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return (T)
                Activator.CreateInstance(
                    typeToConvert,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new object[] { reader.GetString()! },
                    null
                )!;
        }

        throw new JsonException($"Invalid JSON format for {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public sealed class StringValueStructConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsValueType)
            return false;

        var properties = typeToConvert.GetProperties();
        if (properties.Length != 1 || properties[0].PropertyType != typeof(string))
            return false;

        return true;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(StringValueStructConverter<>).MakeGenericType(typeToConvert);
        return (Activator.CreateInstance(converterType) as JsonConverter)!;
    }
}
