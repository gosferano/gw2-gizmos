using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.Gw2Api.NullabilityAudit;

// Reports the JSON keys in the sampled responses that no contract property binds to, by the client's own rule: a
// property's JSON name is its [JsonPropertyName], else its snake_case name, matched case-insensitively. A key the
// contract spells differently (e.g. the API's "AsuraMale" against the policy's "asura_male") is silently dropped by
// the client; this finds them. A dictionary property's keys are data, so they're skipped over.
internal static class Keys
{
    public static void Run()
    {
        Assembly asm = typeof(Gw2Gizmos.Gw2Api.Contract.V2.Items.Item).Assembly;
        const string ns = "Gw2Gizmos.Gw2Api.Contract.";
        Type[] all = asm.GetTypes();

        string reportPath = Path.Combine(AppContext.BaseDirectory, "nullability-report.json");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reportPath));

        int unbound = 0;
        foreach (JsonProperty ep in doc.RootElement.EnumerateObject())
        {
            if (!ep.Value.TryGetProperty("allFields", out JsonElement fields)
                || !Apply.EndpointType.TryGetValue(ep.Name, out string? typeName)
                || asm.GetType(ns + typeName) is not { } root)
            {
                continue;
            }

            List<string> missing = fields.EnumerateArray()
                .Select(field => field.GetString()!)
                .Where(path => !Binds(root, path.Split('.'), 0, all))
                .ToList();
            // Report a key once, not again for each field under it.
            List<string> tops = missing
                .Where(path =>
                    !missing.Any(other => other != path && path.StartsWith(other + ".", StringComparison.Ordinal))
                )
                .ToList();
            foreach (string path in tops)
            {
                Console.WriteLine($"{ep.Name}: {path}");
            }

            unbound += tops.Count;
        }

        Console.WriteLine($"\n{unbound} unbound key path(s).");
    }

    private static bool Binds(Type type, string[] path, int idx, Type[] all)
    {
        if (idx == path.Length)
        {
            return true;
        }

        (string name, string? disc) = Apply.ParseSeg(path[idx]);
        if (name.Length == 0)
        {
            return Apply.NarrowByDisc(type, disc, all).Any(t => Binds(t, path, idx + 1, all));
        }

        foreach (Type t in Family(type, all))
        {
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Name);
                if (!string.Equals(jsonName, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (DictionaryValue(prop.PropertyType) is { } value)
                {
                    // The next segment is a key (data); the one after it is a property of the value.
                    if (idx + 1 >= path.Length - 1)
                    {
                        return true;
                    }

                    (_, string? keyDisc) = Apply.ParseSeg(path[idx + 1]);
                    Type element = Apply.ElementType(value);
                    if (Apply.NarrowByDisc(element, keyDisc, all).Any(c => Binds(c, path, idx + 2, all)))
                    {
                        return true;
                    }

                    continue;
                }

                Type elementType = Apply.ElementType(prop.PropertyType);
                if (Apply.NarrowByDisc(elementType, disc, all).Any(c => Binds(c, path, idx + 1, all)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The type and its subtypes (a polymorphic key may live on any of them).
    private static IEnumerable<Type> Family(Type type, Type[] all) =>
        all.Where(t => type.IsAssignableFrom(t)).Prepend(type).Distinct();

    private static Type? DictionaryValue(Type t) =>
        t.GetInterfaces().Append(t)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            ?.GetGenericArguments()[1];
}
