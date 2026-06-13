using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.State;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Stores how many minutes before an event a reminder fires. Persisted in the shared <see cref="AppState"/>
/// table, cached in memory and written through on change. The Events screen binds the lead-time dropdown to
/// it; <see cref="EventReminderService"/> reads it on each poll so a change applies live.
/// </summary>
public sealed class ReminderSettingsStore
{
    private const string StateKey = "gw2gizmos.reminder.leadtime";
    public const int DefaultLeadTimeMinutes = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private int _leadTimeMinutes;

    public ReminderSettingsStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _leadTimeMinutes = Load();
    }

    public int LeadTimeMinutes
    {
        get
        {
            lock (_gate)
            {
                return _leadTimeMinutes;
            }
        }
        set
        {
            lock (_gate)
            {
                if (_leadTimeMinutes == value)
                {
                    return;
                }

                _leadTimeMinutes = value;
                Save(value);
            }
        }
    }

    private int Load()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        return row is not null && int.TryParse(row.Value, out int minutes) && minutes > 0
            ? minutes
            : DefaultLeadTimeMinutes;
    }

    private void Save(int minutes)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        if (row is null)
        {
            dbContext.AppState.Add(new AppState { Key = StateKey, Value = minutes.ToString() });
        }
        else
        {
            row.Value = minutes.ToString();
        }

        dbContext.SaveChanges();
    }
}
