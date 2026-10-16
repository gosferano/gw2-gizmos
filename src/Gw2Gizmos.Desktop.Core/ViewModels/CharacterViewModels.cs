using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Characters hub: a card per character of the selected account (<see cref="SelectedAccountService"/>),
/// each opening that character's page. Refreshes in place after each account sync. The names come from the
/// synced bag snapshots, so the list is empty until the (off-by-default) Character inventories sync has run.
/// </summary>
public sealed class CharactersViewModel : ViewModelBase, INavigationAware
{
    private readonly AccountReader _reader;
    private readonly string? _accountId;
    private readonly IDisposable _dataChanges;
    private readonly SelectedCharacterService _selected;
    private readonly INavigationService _navigation;
    private string _status = "";

    public CharactersViewModel(
        AccountReader reader,
        SelectedAccountService account,
        SelectedCharacterService selected,
        INavigationService navigation,
        DataChangeMonitor dataChanges)
    {
        _reader = reader;
        _accountId = account.AccountId;
        _selected = selected;
        _navigation = navigation;
        HasAccount = !string.IsNullOrEmpty(account.AccountId);
        OpenCommand = new RelayCommand<string>(Open);

        _ = LoadAsync();
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync()));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<CharacterCard> Characters { get; } = new();

    /// <summary>False when no account is selected; the page shows a "pick an account" note instead.</summary>
    public bool HasAccount { get; }

    /// <summary>True once at least one character has synced; otherwise the page shows <see cref="Status"/>.</summary>
    public bool HasCharacters => Characters.Count > 0;

    /// <summary>Empty-state note shown when an account is selected but no characters have synced yet.</summary>
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    /// <summary>Opens a character's page (the card binds this with the character name as the parameter).</summary>
    public RelayCommand<string> OpenCommand { get; }

    private async Task LoadAsync()
    {
        if (_accountId is not { } accountId)
        {
            return;
        }

        try
        {
            List<CharacterCard> cards = await Task.Run(() => _reader.GetCharacterCards(accountId));
            Characters.ReplaceWith(cards);

            OnPropertyChanged(nameof(HasCharacters));
            Status = cards.Count == 0
                ? "No characters synced yet. Enable “Characters” in Settings (the API key needs the characters and inventories permissions)."
                : "";
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }

    private void Open(string? characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            return;
        }

        _selected.Select(characterName);
        _navigation.NavigateTo(AppPage.Character);
    }
}

/// <summary>Backs a single character's page — its card over its (currently one) sub-section card.</summary>
public sealed class CharacterViewModel : ViewModelBase, INavigationAware
{
    private readonly IDisposable _dataChanges;
    private CharacterCard? _card;

    public CharacterViewModel(
        AccountReader reader, SelectedAccountService account, SelectedCharacterService selected, DataChangeMonitor dataChanges)
    {
        CharacterName = selected.CharacterName ?? "";
        Breadcrumbs = CharacterBreadcrumbs.Character(CharacterName);
        _ = LoadAsync(reader, account.AccountId);
        _dataChanges = dataChanges.Subscribe(AccountPageRefresh.When(() => _ = LoadAsync(reader, account.AccountId)));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public string CharacterName { get; }

    public BreadcrumbEntry[] Breadcrumbs { get; }

    /// <summary>The character's card; null until loaded (or if the character isn't synced).</summary>
    public CharacterCard? Card
    {
        get => _card;
        private set => SetProperty(ref _card, value);
    }

    private async Task LoadAsync(AccountReader reader, string? accountId)
    {
        if (accountId is null || string.IsNullOrEmpty(CharacterName))
        {
            return;
        }

        try
        {
            Card = await Task.Run(() => reader.GetCharacterCard(accountId, CharacterName));
        }
        catch (Exception)
        {
            // Leave as is on a read failure.
        }
    }
}

/// <summary>Backs a character's inventory page: its bag layout as a slot grid (mirrors the bank/shared grid).</summary>
public sealed class CharacterInventoryViewModel : ViewModelBase, INavigationAware
{
    private readonly IDisposable _dataChanges;

    public CharacterInventoryViewModel(
        AccountReader reader, SelectedAccountService account, SelectedCharacterService selected, DataChangeMonitor dataChanges)
    {
        string characterName = selected.CharacterName ?? "";
        Breadcrumbs = CharacterBreadcrumbs.Inventory(characterName);
        _ = LoadAsync(reader, account.AccountId, characterName);
        _dataChanges = dataChanges.Subscribe(
            AccountPageRefresh.When(() => _ = LoadAsync(reader, account.AccountId, characterName)));
    }

    public void OnNavigatedFrom() => _dataChanges.Dispose();

    public ObservableCollection<SlotRow> Slots { get; } = new();

    public BreadcrumbEntry[] Breadcrumbs { get; }

    private async Task LoadAsync(AccountReader reader, string? accountId, string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            return;
        }

        try
        {
            List<SlotRow> slots = await Task.Run(() =>
            {
                string? id = accountId ?? reader.GetCurrentAccount()?.Id;
                return id is null ? new List<SlotRow>() : reader.GetCharacterInventory(id, characterName);
            });

            Slots.ReplaceWith(slots);
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}
