using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// Back and forward through the pages visited, like a browser, each page reopening on what it showed: the selected
/// character or session, or the item it opened on.
/// </summary>
public sealed class NavigationHistoryTests
{
    private readonly SelectedCharacterService _character = new();
    private readonly SelectedSessionService _session = new();
    private readonly PageTargets _targets = new();
    private readonly NavigationHistory _history;

    public NavigationHistoryTests()
    {
        _history = new NavigationHistory(_character, _session, _targets);
    }

    [Fact]
    public void Back_and_forward_step_through_the_pages_visited()
    {
        _history.Record(AppPage.Dashboard);
        _history.Record(AppPage.Items);
        _history.Record(AppPage.Events);

        Assert.Equal(AppPage.Items, _history.Back());
        Assert.Equal(AppPage.Dashboard, _history.Back());
        Assert.Null(_history.Back());
        Assert.Equal(AppPage.Items, _history.Forward());
        Assert.Equal(AppPage.Events, _history.Forward());
        Assert.Null(_history.Forward());
    }

    [Fact]
    public void A_new_page_after_going_back_drops_what_was_ahead()
    {
        _history.Record(AppPage.Dashboard);
        _history.Record(AppPage.Items);
        _history.Back();

        _history.Record(AppPage.Wallet);

        Assert.False(_history.CanGoForward);
        Assert.Equal(AppPage.Dashboard, _history.Back());
    }

    [Fact]
    public void Going_back_restores_the_character_the_page_showed()
    {
        _character.Select("Ghast Icefiend");
        _history.Record(AppPage.Character);
        _character.Select("Party Plants");
        _history.Record(AppPage.Character);

        Assert.Equal(AppPage.Character, _history.Back());

        Assert.Equal("Ghast Icefiend", _character.CharacterName);
    }

    [Fact]
    public void Going_back_restores_the_session_and_segment()
    {
        _session.SelectSession(1, "Monday");
        _session.SelectSegment(10, "Ghast Icefiend", isActive: false);
        _history.Record(AppPage.SessionLoot);
        _session.SelectSession(2, "Tuesday");
        _session.SelectSegment(20, "Party Plants", isActive: true);
        _history.Record(AppPage.SessionLoot);

        _history.Back();

        Assert.Equal((1L, "Monday"), (_session.GameSessionId!.Value, _session.GameSessionTitle));
        Assert.Equal((10L, "Ghast Icefiend", false), (_session.SegmentId!.Value, _session.SegmentTitle,
            _session.SegmentIsActive));
    }

    [Fact]
    public void Going_back_to_a_page_opened_on_an_item_opens_it_on_that_item_again()
    {
        _targets.OpenItem(19721);
        _history.Record(AppPage.Items);
        Assert.Equal(19721, _targets.TakeItem()); // the page takes it as it opens
        _history.Record(AppPage.Dashboard);

        _history.Back();

        Assert.Equal(19721, _targets.TakeItem());
    }

    [Fact]
    public void Reopening_the_page_shown_on_the_same_context_isnt_a_new_entry()
    {
        _history.Record(AppPage.Dashboard);
        _history.Record(AppPage.Items);
        _history.Record(AppPage.Items);

        Assert.Equal(AppPage.Dashboard, _history.Back());
    }

    [Fact]
    public void Changed_is_raised_as_back_and_forward_become_possible()
    {
        int raised = 0;
        _history.Changed += (_, _) => raised++;

        _history.Record(AppPage.Dashboard);
        Assert.False(_history.CanGoBack);
        _history.Record(AppPage.Items);
        Assert.True(_history.CanGoBack);
        _history.Back();
        Assert.True(_history.CanGoForward);

        Assert.Equal(3, raised);
    }
}
