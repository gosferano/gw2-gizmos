using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>Keeping the WvW map from going back in time when the API answers from older data.</summary>
public sealed class WvwMatchReconcilerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 18, 15, 0, 0, TimeSpan.Zero);

    private static WvwMatchMapObjective Objective(string id, WvwOwner owner, int minutes) =>
        new()
        {
            Id = id,
            Owner = owner,
            LastFlipped = T0.AddMinutes(minutes),
        };

    private static WvwMatch Match(string id, params WvwMatchMapObjective[] objectives) =>
        new()
        {
            Id = id,
            Maps = [new Gw2Gizmos.Gw2Api.Contract.V2.Wvw.WvwMatchMap { Id = 38, Objectives = objectives }],
        };

    [Fact]
    public void A_response_whose_newest_flip_is_older_than_the_one_shown_is_kept_out()
    {
        WvwMatch shown = Match("2-1", Objective("a", WvwOwner.Green, 30), Objective("b", WvwOwner.Red, 10));
        WvwMatch stale = Match("2-1", Objective("a", WvwOwner.Blue, 5), Objective("b", WvwOwner.Red, 10));

        Assert.Same(shown, WvwMatchReconciler.Reconcile(shown, stale));
    }

    [Fact]
    public void In_a_newer_response_an_objective_flipped_longer_ago_keeps_its_known_state()
    {
        WvwMatch shown = Match("2-1", Objective("a", WvwOwner.Green, 30), Objective("b", WvwOwner.Red, 10));
        WvwMatch newer = Match("2-1", Objective("a", WvwOwner.Blue, 5), Objective("b", WvwOwner.Green, 40));

        WvwMatch result = WvwMatchReconciler.Reconcile(shown, newer);

        WvwMatchMapObjective[] objectives = result.Maps[0].Objectives;
        Assert.Equal(WvwOwner.Green, objectives.Single(o => o.Id == "a").Owner);
        Assert.Equal(WvwOwner.Green, objectives.Single(o => o.Id == "b").Owner);
        Assert.Equal(T0.AddMinutes(40), objectives.Single(o => o.Id == "b").LastFlipped);
    }

    [Fact]
    public void A_different_match_or_the_first_one_is_taken_as_it_is()
    {
        WvwMatch first = Match("2-1", Objective("a", WvwOwner.Green, 30));
        WvwMatch nextWeek = Match("2-3", Objective("a", WvwOwner.Blue, 1));

        Assert.Same(first, WvwMatchReconciler.Reconcile(null, first));
        Assert.Same(nextWeek, WvwMatchReconciler.Reconcile(first, nextWeek));
    }
}
