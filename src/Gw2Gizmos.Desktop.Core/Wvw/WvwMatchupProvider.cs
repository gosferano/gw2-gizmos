using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract.V2.Account;
using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A matchup, or why there isn't one to show.</summary>
public sealed record WvwMatchupResult(WvwMatchup? Matchup, string? Problem)
{
    public static WvwMatchupResult Failed(string problem) => new(null, problem);
}

/// <summary>
/// Fetches the selected account's WvW matchup live from the API: the account's team (with its key, remembered per
/// account) and the match that team is in. The maps' places on the Mists come from the Maps table the worker syncs.
/// </summary>
public sealed class WvwMatchupProvider
{
    private readonly IGw2ApiClientFactory _clients;
    private readonly FileGw2ApiKeyStore _keys;
    private readonly SelectedAccountService _account;
    private readonly IServiceScopeFactory _scopeFactory;
    private (string AccountId, int Team)? _team;

    /// <summary>Why the latest team lookup found no team, for the page to say.</summary>
    private string? _problem;

    public WvwMatchupProvider(
        IGw2ApiClientFactory clients,
        FileGw2ApiKeyStore keys,
        SelectedAccountService account,
        IServiceScopeFactory scopeFactory
    )
    {
        _clients = clients;
        _keys = keys;
        _account = account;
        _scopeFactory = scopeFactory;
    }

    public async Task<WvwMatchupResult> LoadAsync()
    {
        try
        {
            if (await TeamAsync() is not { } team)
            {
                return WvwMatchupResult.Failed(_problem ?? "Couldn't find your WvW team.");
            }

            Gw2ApiClient client = _clients.Create(Locale.English);
            WvwMatchOverview? overview = await client.V2.Wvw.Matches.Overview.WithWorldId(team).GetBlob();
            if (overview is null)
            {
                return WvwMatchupResult.Failed(
                    "Your team isn't in a match right now (the weekly reset may be under way)."
                );
            }

            WvwMatch? match = await client.V2.Wvw.Matches.GetById(overview.Id);
            if (match is null)
            {
                return WvwMatchupResult.Failed("Couldn't load the match. Try again shortly.");
            }

            return new WvwMatchupResult(
                WvwMatchup.From(match, team, await MapRectsAsync(match), DateTimeOffset.UtcNow),
                null
            );
        }
        catch (Exception)
        {
            return WvwMatchupResult.Failed("Couldn't reach the GW2 API. Try again shortly.");
        }
    }

    /// <summary>The selected account's team, fetched once per account with its key.</summary>
    private async Task<int?> TeamAsync()
    {
        _problem = null;
        string? accountId = _account.AccountId;
        if (accountId is null)
        {
            _problem = "Add an API key to see your WvW matchup.";
            return null;
        }

        if (_team is { } known && known.AccountId == accountId)
        {
            return known.Team;
        }

        string? key = _keys
            .GetStoredKeys()
            .FirstOrDefault(k => string.Equals(k.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
            ?.Key;
        if (key is null)
        {
            _problem = "Add an API key to see your WvW matchup.";
            return null;
        }

        var result = await _clients.Create(key).V2.Account.Wvw.GetBlob();
        if (result.Content is not { Team: > 0 } wvw)
        {
            _problem = result.Error is { } error
                ? $"Couldn't read your WvW team: {error.Text}"
                : "Your account isn't on a WvW team yet.";
            return null;
        }

        _team = (accountId, wvw.Team);
        return wvw.Team;
    }

    /// <summary>The match's maps' rectangles on the Mists, from the Maps table.</summary>
    private Task<Dictionary<int, (double MinX, double MinY, double MaxX, double MaxY)>> MapRectsAsync(WvwMatch match) =>
        Task.Run(() =>
        {
            int[] ids = match.Maps.Select(m => m.Id).ToArray();
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            return db
                .Maps.AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .AsEnumerable()
                .ToDictionary(
                    m => m.Id,
                    m =>
                        (
                            (double)m.ContinentRect.Min.X,
                            (double)m.ContinentRect.Min.Y,
                            (double)m.ContinentRect.Max.X,
                            (double)m.ContinentRect.Max.Y
                        )
                );
        });
}
