using System.Reflection;

namespace Gw2Gizmos.Data.Static.Crafting;

/// <summary>Opens the curated JSON tables embedded in this assembly (see the .csproj EmbeddedResource).</summary>
internal static class EmbeddedData
{
    public static Stream Open(string fileName)
    {
        Assembly assembly = typeof(EmbeddedData).Assembly;
        string name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{fileName}' not found in {assembly.GetName().Name}.");
        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource stream '{name}' could not be opened.");
    }
}
