using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// One side's number, coloured as the side; a total of all three when <paramref name="Side"/> is null.
/// </summary>
public sealed record WvwSideNumber(WvwColor? Side, string Text)
{
    public bool IsRed => Side == WvwColor.Red;
    public bool IsBlue => Side == WvwColor.Blue;
    public bool IsGreen => Side == WvwColor.Green;
    public bool IsTotal => Side is null;
}

/// <summary>
/// A map's line in the WvW side panel: each side's kills, deaths and captures lately with their totals, and how many
/// the account's team can take. Selecting it filters the flip feed to the map.
/// </summary>
public sealed class WvwMapStatsRow : ViewModelBase
{
    private IReadOnlyList<WvwSideNumber> _recentKills = [];
    private IReadOnlyList<WvwSideNumber> _recentDeaths = [];
    private IReadOnlyList<WvwSideNumber> _recentFlips = [];
    private int _capturable;
    private bool _isSelected;

    public WvwMapStatsRow(WvwMatchMap map)
    {
        Map = map;
    }

    public WvwMatchMap Map { get; private set; }

    public string Name => Map.Name;
    public bool IsRed => Map.Home == WvwColor.Red;
    public bool IsBlue => Map.Home == WvwColor.Blue;
    public bool IsGreen => Map.Home == WvwColor.Green;

    public IReadOnlyList<WvwSideNumber> RecentKills
    {
        get => _recentKills;
        private set => SetProperty(ref _recentKills, value);
    }

    public IReadOnlyList<WvwSideNumber> RecentDeaths
    {
        get => _recentDeaths;
        private set => SetProperty(ref _recentDeaths, value);
    }

    /// <summary>Objectives each side took within the activity window, then all flips.</summary>
    public IReadOnlyList<WvwSideNumber> RecentFlips
    {
        get => _recentFlips;
        private set => SetProperty(ref _recentFlips, value);
    }

    /// <summary>Objectives the account's team can take now.</summary>
    public int Capturable
    {
        get => _capturable;
        private set => SetProperty(ref _capturable, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void Refresh(WvwMatchup matchup, WvwActivity activity, DateTimeOffset now)
    {
        Map = matchup.Maps.FirstOrDefault(m => m.MapId == Map.MapId) ?? Map;
        IReadOnlyList<WvwColor> sides = matchup.Teams.Select(t => t.Color).ToList();
        var recent = sides.Select(side => (Side: side, Counts: activity.Recent(Map.MapId, side))).ToList();
        RecentKills = WithTotal(recent.Select(r => (r.Side, r.Counts?.Kills)));
        RecentDeaths = WithTotal(recent.Select(r => (r.Side, r.Counts?.Deaths)));
        RecentFlips = WithTotal(
            sides.Select(side => (side, (int?)activity.FlippedLately(matchup, Map.MapId, now, side))),
            activity.FlippedLately(matchup, Map.MapId, now)
        );
        WvwColor? yours = matchup.Teams.FirstOrDefault(t => t.IsYours)?.Color;
        Capturable = yours is { } team ? WvwActivity.Capturable(matchup, Map.MapId, team, now) : 0;
    }

    /// <summary>
    /// Each side's count, then the total: <paramref name="total"/>, or the sides' sum (unknown while any side's is).
    /// </summary>
    private static List<WvwSideNumber> WithTotal(IEnumerable<(WvwColor Side, int? Count)> counts, int? total = null)
    {
        var bySide = counts.ToList();
        int? sum = total ?? (bySide.All(c => c.Count is not null) ? bySide.Sum(c => c.Count!.Value) : null);
        return [.. bySide.Select(c => new WvwSideNumber(c.Side, Count(c.Count))), new WvwSideNumber(null, Count(sum))];
    }

    /// <summary>The count, or "–" while there isn't enough history for one.</summary>
    private static string Count(int? count) => count?.ToString(CultureInfo.CurrentCulture) ?? "–";
}

/// <summary>A flip in the WvW side panel's feed, with how long ago it was.</summary>
public sealed class WvwFlipRow : ViewModelBase
{
    private string _ago = "";

    public WvwFlipRow(WvwFlip flip)
    {
        Flip = flip;
    }

    public WvwFlip Flip { get; }

    public string ObjectiveName => Flip.ObjectiveName;
    public string MapName => Flip.MapName;

    /// <summary>E.g. "Green → Blue", "→ Blue" when who held it before isn't known, or "Blue retook it".</summary>
    public string Change =>
        !Flip.FromKnown ? $"→ {Side(Flip.To)}"
        : Flip.From == Flip.To ? $"{Side(Flip.To)} retook it"
        : $"{Side(Flip.From)} → {Side(Flip.To)}";

    public bool ToRed => Flip.To == WvwColor.Red;
    public bool ToBlue => Flip.To == WvwColor.Blue;
    public bool ToGreen => Flip.To == WvwColor.Green;

    /// <summary>E.g. "now", "3 min ago", "1h 05m ago".</summary>
    public string Ago
    {
        get => _ago;
        private set => SetProperty(ref _ago, value);
    }

    public void Refresh(DateTimeOffset now)
    {
        TimeSpan since = now - Flip.At;
        Ago =
            since < TimeSpan.FromMinutes(1) ? "now"
            : since < TimeSpan.FromHours(1) ? $"{(int)since.TotalMinutes} min ago"
            : $"{(int)since.TotalHours}h {since.Minutes:00}m ago";
    }

    private static string Side(WvwColor? side) => side?.ToString() ?? "Neutral";
}
