using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Skins;

/// <summary>
/// An armor skin's dye slots for a race and gender, where they differ from the default. The API spells these keys in
/// PascalCase ("AsuraMale"), which the contract's snake_case naming would never match, so each is named explicitly. A
/// slot is null where it can't be dyed.
/// </summary>
public sealed class SkinArmorDetailsDyeSlotsOverrides
{
    [JsonPropertyName("AsuraMale")]
    public SkinDyeSlot?[] AsuraMale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("AsuraFemale")]
    public SkinDyeSlot?[] AsuraFemale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("CharrMale")]
    public SkinDyeSlot?[] CharrMale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("CharrFemale")]
    public SkinDyeSlot?[] CharrFemale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("HumanMale")]
    public SkinDyeSlot?[] HumanMale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("HumanFemale")]
    public SkinDyeSlot?[] HumanFemale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("NornMale")]
    public SkinDyeSlot?[] NornMale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("NornFemale")]
    public SkinDyeSlot?[] NornFemale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("SylvariMale")]
    public SkinDyeSlot?[] SylvariMale { get; set; } = Array.Empty<SkinDyeSlot?>();

    [JsonPropertyName("SylvariFemale")]
    public SkinDyeSlot?[] SylvariFemale { get; set; } = Array.Empty<SkinDyeSlot?>();
}
