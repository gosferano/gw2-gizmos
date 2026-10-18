using System;
using System.Linq;
using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Keeps the WvW map from going back in time. Consecutive API responses don't always come from the same data: measured,
/// one response in a few minutes changed dozens of objectives at once to flips up to an hour old. A real match only
/// ever gains newer flips, so a response whose newest flip is older than the newest already shown is stale and kept
/// out; in one that's newer, an objective whose flip is older than the one shown keeps the state already known.
/// </summary>
public static class WvwMatchReconciler
{
    /// <summary>The match to show given the one shown so far (<paramref name="shown"/>) and a new response.</summary>
    public static WvwMatch Reconcile(WvwMatch? shown, WvwMatch incoming)
    {
        if (shown is null || shown.Id != incoming.Id)
        {
            return incoming;
        }

        if (NewestFlip(incoming) < NewestFlip(shown))
        {
            return shown;
        }

        var known = shown.Maps.SelectMany(m => m.Objectives).ToDictionary(o => o.Id);
        foreach (var map in incoming.Maps)
        {
            for (int i = 0; i < map.Objectives.Length; i++)
            {
                if (
                    known.TryGetValue(map.Objectives[i].Id, out WvwMatchMapObjective? before)
                    && before.LastFlipped > map.Objectives[i].LastFlipped
                )
                {
                    map.Objectives[i] = before;
                }
            }
        }

        return incoming;
    }

    private static DateTimeOffset NewestFlip(WvwMatch match) =>
        match.Maps.SelectMany(m => m.Objectives).Select(o => o.LastFlipped).DefaultIfEmpty().Max();
}
