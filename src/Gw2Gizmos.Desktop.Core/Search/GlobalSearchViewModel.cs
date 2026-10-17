using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Ui;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the global search overlay (Ctrl+K): the query, its results (searched as typing pauses), the highlighted
/// result moved with the arrow keys, and opening a result — selecting its character or setting the page's target
/// first where needed, then navigating.
/// </summary>
public sealed class GlobalSearchViewModel : ViewModelBase
{
    private readonly GlobalSearch _search;
    private readonly INavigationService _navigation;
    private readonly SelectedCharacterService _character;
    private readonly PageTargets _targets;
    private readonly IUiTimer _debounce;
    private CancellationTokenSource? _searching;
    private string _query = "";
    private bool _isOpen;
    private SearchResult? _selectedResult;
    private bool _noResults;

    public GlobalSearchViewModel(
        GlobalSearch search,
        INavigationService navigation,
        SelectedCharacterService character,
        PageTargets targets,
        IUiDispatcher dispatcher)
    {
        _search = search;
        _navigation = navigation;
        _character = character;
        _targets = targets;
        _debounce = dispatcher.CreateTimer(TimeSpan.FromMilliseconds(150), OnTypingPaused);
    }

    private void OnTypingPaused()
    {
        _debounce.Stop();
        _ = RunSearchAsync();
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                _debounce.Stop();
                _debounce.Start();
            }
        }
    }

    public ObservableCollection<SearchResult> Results { get; } = new();

    public SearchResult? SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    /// <summary>Something was typed and nothing matched.</summary>
    public bool NoResults
    {
        get => _noResults;
        private set => SetProperty(ref _noResults, value);
    }

    /// <summary>Opens the overlay with an empty query (listing the pages) and reads the in-memory sources fresh.</summary>
    public async Task OpenAsync()
    {
        _query = "";
        OnPropertyChanged(nameof(Query));
        IsOpen = true;
        await RunSearchAsync();
        await _search.PrepareAsync();
    }

    public void Close()
    {
        _debounce.Stop();
        _searching?.Cancel();
        IsOpen = false;
    }

    /// <summary>Moves the highlight up or down the results, wrapping at the ends.</summary>
    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            return;
        }

        int index = SelectedResult is null ? -1 : Results.IndexOf(SelectedResult);
        index = index < 0 ? (delta > 0 ? 0 : Results.Count - 1) : (index + delta + Results.Count) % Results.Count;
        SelectedResult = Results[index];
    }

    /// <summary>Opens <paramref name="result"/> (or the highlighted one) and closes the overlay.</summary>
    public void Activate(SearchResult? result = null)
    {
        result ??= SelectedResult;
        if (result is null)
        {
            return;
        }

        Close();
        switch (result.Kind)
        {
            case SearchResultKind.Page when result.Page is { } page:
                _navigation.NavigateTo(page);
                break;
            case SearchResultKind.Character when result.CharacterName is { } name:
                _character.Select(name);
                _navigation.NavigateTo(AppPage.Character, reload: true);
                break;
            case SearchResultKind.Item when result.ItemId is { } itemId:
                _targets.OpenItem(itemId);
                _navigation.NavigateTo(AppPage.Items, reload: true);
                break;
            case SearchResultKind.Event when result.EventName is { } eventName:
                _targets.OpenEvents(eventName);
                _navigation.NavigateTo(AppPage.Events, reload: true);
                break;
        }
    }

    private async Task RunSearchAsync()
    {
        _searching?.Cancel();
        var searching = new CancellationTokenSource();
        _searching = searching;
        string query = _query;
        try
        {
            List<SearchResult> results = await _search.SearchAsync(query, searching.Token);
            if (searching.IsCancellationRequested)
            {
                return;
            }

            Results.ReplaceWith(results);
            SelectedResult = Results.Count > 0 ? Results[0] : null;
            NoResults = query.Trim().Length > 0 && Results.Count == 0;
        }
        catch (OperationCanceledException)
        {
            // A newer query took over.
        }
        catch (Exception)
        {
            // A failed lookup (e.g. the database not ready) leaves the last results.
        }
    }
}
