using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

/// <summary>
/// The WvW team each guild of a region is on, by guild id. The API sends the team ids as strings.
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class WvwGuildTeams : Dictionary<string, int>;
