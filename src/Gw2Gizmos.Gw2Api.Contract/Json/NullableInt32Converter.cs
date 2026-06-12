using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.Json;

/// <summary>
/// Reads a nullable int that the GW2 API sometimes returns as a string. The clearest case is
/// <c>secondary_suffix_item_id</c>, which is an empty string when absent and a numeric string when
/// present, while the contract types it as <see cref="int"/>?. Empty/whitespace strings and JSON
/// null map to null; numbers and numeric strings parse normally — so this is a safe superset of the
/// built-in behaviour (genuine numbers are unaffected).
/// </summary>
public sealed class NullableInt32Converter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.String:
                string? value = reader.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, CultureInfo.InvariantCulture);
            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' when reading a nullable int.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
