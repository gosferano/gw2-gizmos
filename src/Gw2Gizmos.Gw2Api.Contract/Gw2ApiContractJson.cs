using System.Text.Json;
using Gw2Gizmos.Gw2Api.Contract.Json;

namespace Gw2Gizmos.Gw2Api.Contract;

/// <summary>
/// JSON (de)serialization for the GW2 API v2 contract types. Use <see cref="Options"/> with
/// <see cref="JsonSerializer"/> to read or write the DTOs — it registers the polymorphic converters
/// (items, skins, skill/trait facts, …), the snake_case naming policy, and the source-generated type
/// metadata. This lets consumers of the contract package deserialize API responses on their own,
/// without taking the HTTP client.
/// </summary>
public static class Gw2ApiContractJson
{
    private static readonly JsonSerializerOptions Configured = new()
    {
        Converters =
        {
            new StringValueStructConverterFactory(),
            new NullableInt32Converter(),
            new ItemJsonConverter(),
            new GuildUpgradeJsonConverter(),
            new ProfessionTrainingTrackStepJsonConverter(),
            new SkillFactJsonConverter(),
            new SkinJsonConverter(),
            new TraitFactJsonConverter(),
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        TypeInfoResolver = Gw2ApiV2JsonContext.Default,
    };

    /// <summary>Serializer options for the GW2 API v2 contract types.</summary>
    public static JsonSerializerOptions Options { get; } = new Gw2ApiV2JsonContext(Configured).Options;
}
