using System.Linq;
using Gw2Gizmos.Data.EntityFramework;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// The database provider chosen at launch via the <c>Database:Provider</c> setting (default <c>sqlite</c>),
/// resolved from the providers registered at the composition root. Both the provider key and the connection
/// string come from configuration (launch arg / env var) because the database can't be opened to read them.
/// </summary>
public sealed class ActiveDbProvider
{
    public const string DefaultProviderKey = "sqlite";

    public ActiveDbProvider(IEnumerable<IGw2GizmosDbProvider> providers, IConfiguration configuration)
    {
        string? key = configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(key))
        {
            key = DefaultProviderKey;
        }

        List<IGw2GizmosDbProvider> available = providers.ToList();
        Provider =
            available.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No database provider registered for Database:Provider='{key}'. Registered: "
                    + $"{(available.Count == 0 ? "(none)" : string.Join(", ", available.Select(p => p.Key)))}. "
                    + "Register it at the composition root (e.g. AddGw2GizmosSqlite())."
            );
    }

    public IGw2GizmosDbProvider Provider { get; }
}
