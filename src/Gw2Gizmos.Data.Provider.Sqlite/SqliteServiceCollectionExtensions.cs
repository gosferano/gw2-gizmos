using Gw2Gizmos.Data.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gw2Gizmos.Data.Provider.Sqlite;

public static class SqliteServiceCollectionExtensions
{
    /// <summary>
    /// Makes the SQLite provider available for selection (by <c>Database:Provider=sqlite</c>). Call at the
    /// composition root alongside any other providers, before <c>AddGw2GizmosIngestion</c> /
    /// <c>AddGw2GizmosDeliveryNotifications</c>. Registered additively so multiple providers can coexist and
    /// be chosen at launch.
    /// </summary>
    public static IServiceCollection AddGw2GizmosSqlite(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGw2GizmosDbProvider, SqliteDbProvider>());
        return services;
    }
}
