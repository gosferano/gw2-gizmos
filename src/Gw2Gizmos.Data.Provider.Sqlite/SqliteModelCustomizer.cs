using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gw2Gizmos.Data.Provider.Sqlite;

/// <summary>
/// Stores every <see cref="DateTimeOffset"/> as UTC ticks (a <see cref="long"/>) for SQLite. SQLite has no native
/// DateTimeOffset type, and EF's default TEXT mapping can't be ordered, compared, or aggregated server-side
/// ("SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses"). Converting to a long makes
/// those translate to ordinary integer SQL, so the data layer can filter/order/aggregate by date in the database
/// without any SQLite-specific code leaking out of this provider. Registered via
/// <see cref="SqliteDbProvider.Configure"/> as the <see cref="IModelCustomizer"/>.
/// <para>
/// All persisted values are UTC (<c>DateTimeOffset.UtcNow</c>), so round-tripping through UTC ticks is exact.
/// </para>
/// </summary>
internal sealed class SqliteModelCustomizer : RelationalModelCustomizer
{
    private static readonly ValueConverter<DateTimeOffset, long> TicksConverter = new(
        value => value.UtcTicks,
        ticks => new DateTimeOffset(ticks, TimeSpan.Zero)
    );

    public SqliteModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies) { }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(TicksConverter);
                }
            }
        }
    }
}
