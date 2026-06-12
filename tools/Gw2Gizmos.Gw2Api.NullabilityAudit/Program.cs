using System.Net.Http.Headers;
using System.Text.Json;

// Empirically samples GW2 v2 endpoints and reports, per field path, how often it is present / null /
// absent across real responses — so genuinely-optional DTO fields can be found and made nullable.
//
// Usage: dotnet run --project tools/Gw2Gizmos.Gw2Api.NullabilityAudit -- [endpoint ...]
//   With no args, a default set of game-data endpoints is sampled (no API key needed).
//   GW2_API_KEY (env) is used if present, enabling token-gated endpoints.

// Second mode: map the report onto the DTOs and make the optional (non-collection) fields nullable.
if (args.Length > 0 && args[0] == "apply")
{
    Gw2Gizmos.Gw2Api.NullabilityAudit.Apply.Run(args[1..]);
    return;
}

const string BaseUrl = "https://api.guildwars2.com";
const string SchemaVersion = "2022-03-23T19:00:00.000Z";
const int MaxObjectsPerEndpoint = 4000; // strided sample cap, for speed + polymorphic coverage
const int ChunkSize = 200; // GW2 bulk-expansion limit

string[] DefaultGameData =
[
    "items", "skills", "recipes", "traits", "skins", "colors", "currencies", "minis", "materials",
    "itemstats", "achievements", "achievements/categories", "achievements/groups", "titles",
    "masteries", "mounts/skins", "mounts/types", "novelties", "outfits", "pets", "professions",
    "races", "specializations", "finishers", "gliders", "jadebots", "legends", "mailcarriers",
    "maps", "quests", "raids", "dungeons", "emotes", "worldbosses", "worlds", "backstory/answers",
    "backstory/questions", "stories", "stories/seasons", "pvp/amulets", "pvp/heroes", "pvp/ranks",
    "wvw/abilities", "wvw/objectives", "wvw/ranks", "wvw/upgrades", "guild/upgrades", "guild/permissions",
];

string[] endpoints = args.Length > 0 ? args : DefaultGameData;
string? token = Environment.GetEnvironmentVariable("GW2_API_KEY");

using var http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(100) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Gw2Gizmos-NullabilityAudit");
http.DefaultRequestHeaders.Add("X-Schema-Version", SchemaVersion);
if (!string.IsNullOrWhiteSpace(token))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

var report = new Dictionary<string, object>();

foreach (string endpoint in endpoints)
{
    try
    {
        var stats = new EndpointStats();
        Console.Error.WriteLine($"[{endpoint}] listing ids…");
        string idsBody = await http.GetStringAsync($"/v2/{endpoint}");
        using JsonDocument idsDoc = JsonDocument.Parse(idsBody);

        if (idsDoc.RootElement.ValueKind != JsonValueKind.Array)
        {
            // Single-object ("blob") endpoint — sample it directly.
            Walk(idsDoc.RootElement, "", stats);
            report[endpoint] = Summarize(stats, 1);
            Console.Error.WriteLine($"[{endpoint}] blob endpoint, 1 object");
            continue;
        }

        // An array root is either an id list (game data: bulk-expand via ?ids=) or already-expanded
        // objects (many token-gated endpoints, e.g. account/bank, account/achievements). Peek the first
        // element to tell them apart.
        JsonElement first = default;
        bool any = false;
        foreach (JsonElement e in idsDoc.RootElement.EnumerateArray())
        {
            (first, any) = (e, true);
            break;
        }

        if (!any)
        {
            report[endpoint] = Summarize(stats, 0);
            Console.Error.WriteLine($"[{endpoint}] empty");
            continue;
        }

        if (first.ValueKind == JsonValueKind.Object)
        {
            int walked = 0;
            foreach (JsonElement obj in idsDoc.RootElement.EnumerateArray())
            {
                Walk(obj, "", stats); // Walk ignores null/non-object slots (e.g. empty bank slots)
                walked++;
            }

            report[endpoint] = Summarize(stats, walked);
            Console.Error.WriteLine($"[{endpoint}] inline objects, {walked}");
            continue;
        }

        List<string> ids = idsDoc.RootElement.EnumerateArray().Select(e => e.ToString()).ToList();
        List<string> sample = Stride(ids, MaxObjectsPerEndpoint);

        int sampled = 0;
        foreach (string[] chunk in sample.Chunk(ChunkSize))
        {
            string url = $"/v2/{endpoint}?ids={string.Join(",", chunk.Select(Uri.EscapeDataString))}";
            try
            {
                string body = await http.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement obj in doc.RootElement.EnumerateArray())
                {
                    Walk(obj, "", stats);
                    sampled++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{endpoint}] chunk failed: {ex.Message}");
            }
        }

        report[endpoint] = Summarize(stats, sampled);
        Console.Error.WriteLine($"[{endpoint}] sampled {sampled}/{ids.Count}");
    }
    catch (Exception ex)
    {
        report[endpoint] = new { error = ex.Message };
        Console.Error.WriteLine($"[{endpoint}] FAILED: {ex.Message}");
    }
}

string outPath = Path.Combine(AppContext.BaseDirectory, "nullability-report.json");
await File.WriteAllTextAsync(
    outPath,
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
);
Console.Error.WriteLine($"\nReport written to {outPath}");

// Walks an object, counting per-path occurrences. prefix is the dotted path to `element`'s container.
// Polymorphic objects (those carrying a "type" discriminator) qualify their path with [type] so that
// sibling subtypes are tracked separately — e.g. facts[Buff].duration vs facts[Damage].*, and
// [Armor].details.* vs [Weapon].details.* — instead of being conflated into one absent/present tally.
static void Walk(JsonElement element, string prefix, EndpointStats stats)
{
    if (element.ValueKind != JsonValueKind.Object)
    {
        return;
    }

    if (element.TryGetProperty("type", out JsonElement disc) && disc.ValueKind == JsonValueKind.String)
    {
        prefix = $"{prefix}[{disc.GetString()}]";
    }

    stats.ObjectCount[prefix] = stats.ObjectCount.GetValueOrDefault(prefix) + 1;

    foreach (JsonProperty prop in element.EnumerateObject())
    {
        string path = prefix.Length == 0 ? prop.Name : $"{prefix}.{prop.Name}";
        stats.Present[path] = stats.Present.GetValueOrDefault(path) + 1;

        if (prop.Value.ValueKind == JsonValueKind.Null)
        {
            stats.Null[path] = stats.Null.GetValueOrDefault(path) + 1;
        }
        else if (prop.Value.ValueKind == JsonValueKind.Object)
        {
            Walk(prop.Value, path, stats);
        }
        else if (prop.Value.ValueKind == JsonValueKind.Array)
        {
            // Array elements share the property path; each typed element self-qualifies via its [type].
            foreach (JsonElement item in prop.Value.EnumerateArray())
            {
                Walk(item, path, stats);
            }
        }
    }
}

// Emits, per field path, the fields that are EVER null or EVER absent (relative to their parent).
static object Summarize(EndpointStats stats, int rootObjects)
{
    var optional = new List<object>();
    foreach ((string path, int present) in stats.Present.OrderBy(p => p.Key))
    {
        int lastDot = path.LastIndexOf('.');
        string parent = lastDot < 0 ? "" : path[..lastDot];
        int parents = stats.ObjectCount.GetValueOrDefault(parent);
        int nulls = stats.Null.GetValueOrDefault(path);
        int absent = Math.Max(0, parents - present);

        if (nulls > 0 || absent > 0)
        {
            optional.Add(new
            {
                field = path,
                everNull = nulls > 0,
                everAbsent = absent > 0,
                nullCount = nulls,
                absentCount = absent,
                seen = present,
                ofParents = parents,
            });
        }
    }

    return new { rootObjects, optionalFields = optional };
}

// Evenly samples up to `max` items across the list (covers id range + polymorphic variety).
static List<string> Stride(List<string> ids, int max)
{
    if (ids.Count <= max)
    {
        return ids;
    }

    int stride = ids.Count / max;
    var result = new List<string>(max);
    for (int i = 0; i < ids.Count && result.Count < max; i += stride)
    {
        result.Add(ids[i]);
    }

    return result;
}

sealed class EndpointStats
{
    public Dictionary<string, int> ObjectCount { get; } = new();
    public Dictionary<string, int> Present { get; } = new();
    public Dictionary<string, int> Null { get; } = new();
}
