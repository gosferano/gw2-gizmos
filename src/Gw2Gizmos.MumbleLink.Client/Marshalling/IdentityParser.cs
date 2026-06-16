using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.MumbleLink.Client.Marshalling;

/// <summary>
/// Parses MumbleLink's <c>identity</c> JSON string into a <see cref="MumbleIdentity"/>. Returns <c>null</c> when
/// the string is empty (the game has not populated it yet) or is not valid identity JSON — both expected, not errors.
/// </summary>
internal static class IdentityParser
{
    public static MumbleIdentity? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        IdentityDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<IdentityDto>(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null)
        {
            return null;
        }

        return new MumbleIdentity
        {
            Name = dto.Name ?? "",
            Profession = dto.Profession,
            Spec = dto.Spec,
            Race = dto.Race,
            MapId = dto.MapId,
            WorldId = dto.WorldId,
            TeamColorId = dto.TeamColorId,
            Commander = dto.Commander,
            Fov = dto.Fov,
            UiSize = dto.UiSize,
        };
    }

    // Mirrors the GW2 identity JSON (snake_case keys); deserialized then projected onto the contract DTO so the
    // contract package stays free of any System.Text.Json dependency.
    private sealed class IdentityDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profession")]
        public int Profession { get; set; }

        [JsonPropertyName("spec")]
        public int Spec { get; set; }

        [JsonPropertyName("race")]
        public int Race { get; set; }

        [JsonPropertyName("map_id")]
        public int MapId { get; set; }

        [JsonPropertyName("world_id")]
        public int WorldId { get; set; }

        [JsonPropertyName("team_color_id")]
        public int TeamColorId { get; set; }

        [JsonPropertyName("commander")]
        public bool Commander { get; set; }

        [JsonPropertyName("fov")]
        public float Fov { get; set; }

        [JsonPropertyName("uisz")]
        public int UiSize { get; set; }
    }
}
