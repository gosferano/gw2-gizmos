using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static.Resets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The account's stored progress for the current reset: its once-per-reset activities and its Wizard's Vault, with
/// the names of the items they refer to (daily crafts, meta rewards).
/// </summary>
public sealed record ResetProgressData(
    IReadOnlyList<AccountResetActivity> Activities,
    IReadOnlyList<AccountWizardsVaultObjective> Objectives,
    IReadOnlyList<AccountWizardsVaultMeta> Metas,
    IReadOnlyDictionary<int, string> ItemNames
)
{
    public static ResetProgressData Empty { get; } = new([], [], [], new Dictionary<int, string>());

    /// <summary>Nothing synced yet (the feature is off, the key lacks the permission, or no sync has run).</summary>
    public bool IsEmpty => Activities.Count == 0 && Objectives.Count == 0 && Metas.Count == 0;
}

/// <summary>
/// Read-only access to the worker-synced reset progress for the Daily and weekly page and its Dashboard card. Opens
/// its own short scope per call; meant to run off the UI thread.
/// </summary>
public sealed class ResetProgressReader
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ResetProgressReader(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The account's progress; empty without an account or when nothing has synced.</summary>
    public ResetProgressData Read(string? accountId)
    {
        if (accountId is null)
        {
            return ResetProgressData.Empty;
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            List<AccountResetActivity> activities = db.AccountResetActivities.AsNoTracking()
                .Where(a => a.AccountId == accountId)
                .OrderBy(a => a.Order)
                .ToList();
            List<AccountWizardsVaultObjective> objectives = db.AccountWizardsVaultObjectives.AsNoTracking()
                .Where(o => o.AccountId == accountId)
                .OrderBy(o => o.ObjectiveId)
                .ToList();
            List<AccountWizardsVaultMeta> metas = db.AccountWizardsVaultMetas.AsNoTracking()
                .Where(m => m.AccountId == accountId)
                .ToList();

            HashSet<int> itemIds = ResetActivities.DailyCraftingItems.Values
                .Concat(metas.Select(m => m.RewardItemId))
                .ToHashSet();
            Dictionary<int, string> itemNames = db.Items.AsNoTracking()
                .Where(item => itemIds.Contains(item.Id))
                .Select(item => new { item.Id, item.Name })
                .ToDictionary(item => item.Id, item => item.Name);

            return new ResetProgressData(activities, objectives, metas, itemNames);
        }
        catch (Exception)
        {
            return ResetProgressData.Empty;
        }
    }
}
