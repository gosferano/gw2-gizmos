using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Gw2Gizmos.RecipeFinder.Model;

namespace Gw2Gizmos.RecipeFinder;

/// <summary>
/// Round-trips a <see cref="RecipeNode"/> tree to and from JSON for persistence. Lives here, beside the
/// model, so the worker (which writes the snapshot) and the desktop (which reads it) share one shape.
/// <para>
/// <see cref="RecipeNode"/>'s derived figures (<c>SellPrice</c>, <c>CraftingCost</c>, <c>IsProfitable</c>,
/// …) are getter-only and recompute from the stored fields, so we drop every read-only property at the
/// serialization boundary — the model stays free of persistence attributes and the JSON stays compact.
/// </para>
/// </summary>
public static class RecipeTreeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { DropReadOnlyProperties }
        }
    };

    public static string Serialize(RecipeNode node) => JsonSerializer.Serialize(node, Options);

    public static RecipeNode? Deserialize(string json) => JsonSerializer.Deserialize<RecipeNode>(json, Options);

    private static void DropReadOnlyProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            // A property with no setter is a computed value; persist only the source fields.
            if (typeInfo.Properties[i].Set is null)
            {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }
}
