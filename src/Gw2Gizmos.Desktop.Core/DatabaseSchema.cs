using System;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Whether the database has every migration this build knows. The worker migrates it as it starts, a moment after the
/// desktop's own background readers (dashboard, reminders, the change monitor); a reader that queried a table a new
/// version adds before then would fail, and EF logs that as an error. Readers ask this first and skip until it's
/// current. Checked at most once a second until it is, then remembered.
/// </summary>
public sealed class DatabaseSchema
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private volatile bool _isCurrent;
    private DateTimeOffset _checkedAt = DateTimeOffset.MinValue;

    public DatabaseSchema(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Whether a reader in <paramref name="scope"/> can query now: false only while the app's schema check says the
    /// database isn't current yet (without one registered, e.g. in tests, it can).
    /// </summary>
    public static bool Allows(IServiceScope scope) =>
        scope.ServiceProvider.GetService<DatabaseSchema>() is not { IsCurrent: false };

    /// <summary>The database exists and has no pending migrations.</summary>
    public bool IsCurrent
    {
        get
        {
            if (_isCurrent)
            {
                return true;
            }

            lock (_gate)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (_isCurrent || now - _checkedAt < CheckInterval)
                {
                    return _isCurrent;
                }

                _checkedAt = now;
                _isCurrent = Check();
                return _isCurrent;
            }
        }
    }

    private bool Check()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            return !db.Database.GetPendingMigrations().Any();
        }
        catch (Exception)
        {
            // No database yet (a fresh install, before the worker creates it).
            return false;
        }
    }
}
