using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>One side's number, coloured as the side.</summary>
public sealed record WvwSideNumber(WvwColor Side, string Text)
{
    public bool IsRed => Side == WvwColor.Red;
    public bool IsBlue => Side == WvwColor.Blue;
    public bool IsGreen => Side == WvwColor.Green;
}

/// <summary>
/// A map's line in the WvW side panel: each side's kills and deaths per minute, objectives flipped lately, and how many
/// the account's team can take. Selecting it filters the flip feed to the map.
/// </summary>
public sealed class WvwMapStatsRow : ViewModelBase
{
    private IReadOnlyList<WvwSideNumber> _killsPerMinute = [];
    private IReadOnlyList<WvwSideNumber> _deathsPerMinute = [];
    private int _flippedLately;
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

    public IReadOnlyList<WvwSideNumber> KillsPerMinute
    {
        get => _killsPerMinute;
        private set => SetProperty(ref _killsPerMinute, value);
    }

    public IReadOnlyList<WvwSideNumber> DeathsPerMinute
    {
        get => _deathsPerMinute;
        private set => SetProperty(ref _deathsPerMinute, value);
    }

    /// <summary>Objectives flipped within the last ten minutes.</summary>
    public int FlippedLately
    {
        get => _flippedLately;
        private set => SetProperty(ref _flippedLately, value);
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
        var rates = sides.Select(side => (Side: side, Rate: activity.PerMinute(Map.MapId, side, now))).ToList();
        KillsPerMinute = rates.Select(r => new WvwSideNumber(r.Side, Rate(r.Rate?.Kills))).ToList();
        DeathsPerMinute = rates.Select(r => new WvwSideNumber(r.Side, Rate(r.Rate?.Deaths))).ToList();
        FlippedLately = WvwActivity.FlippedLately(matchup, Map.MapId, now);
        WvwColor? yours = matchup.Teams.FirstOrDefault(t => t.IsYours)?.Color;
        Capturable = yours is { } team ? WvwActivity.Capturable(matchup, Map.MapId, team, now) : 0;
    }

    /// <summary>"3.2", or "–" while there isn't enough history for a rate.</summary>
    private static string Rate(double? perMinute) =>
        perMinute is { } rate ? rate.ToString("0.0", CultureInfo.CurrentCulture) : "–";
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
