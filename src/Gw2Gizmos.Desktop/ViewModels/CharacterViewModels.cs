using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gw2Gizmos.Desktop.Controls;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Characters hub: a card per character of the selected account (<see cref="SelectedAccountService"/>),
/// each opening that character's page. Transient, so the list reloads on navigation. The names come from the
/// synced bag snapshots, so the list is empty until the (off-by-default) Character inventories sync has run.
/// </summary>
public sealed class CharactersViewModel : ViewModelBase
{
    private readonly SelectedCharacterService _selected;
    private string _status = "";

    public CharactersViewModel(AccountReader reader, SelectedAccountService account, SelectedCharacterService selected)
    {
        _selected = selected;
        HasAccount = !string.IsNullOrEmpty(account.AccountId);
        OpenCommand = new RelayCommand<string>(Open);

        if (account.AccountId is { } accountId)
        {
            _ = LoadAsync(reader, accountId);
        }
    }

    public ObservableCollection<string> Characters { get; } = new();

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

    private async Task LoadAsync(AccountReader reader, string accountId)
    {
        try
        {
            List<string> names = await Task.Run(() => reader.GetCharacterNames(accountId));
            foreach (string name in names)
            {
                Characters.Add(name);
            }

            OnPropertyChanged(nameof(HasCharacters));
            Status = names.Count == 0
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
        App.NavigateTo(typeof(CharacterPage));
    }
}

/// <summary>Backs a single character's page — the name header over its (currently one) sub-section card.</summary>
public sealed class CharacterViewModel : ViewModelBase
{
    public CharacterViewModel(SelectedCharacterService selected)
    {
        CharacterName = selected.CharacterName ?? "";
        Breadcrumbs = CharacterBreadcrumbs.Character(CharacterName);
    }

    public string CharacterName { get; }

    public BreadcrumbEntry[] Breadcrumbs { get; }
}

/// <summary>Backs a character's inventory page: its bag layout as a slot grid (mirrors the bank/shared grid).</summary>
public sealed class CharacterInventoryViewModel : ViewModelBase
{
    public CharacterInventoryViewModel(AccountReader reader, SelectedAccountService account, SelectedCharacterService selected)
    {
        string characterName = selected.CharacterName ?? "";
        Breadcrumbs = CharacterBreadcrumbs.Inventory(characterName);
        _ = LoadAsync(reader, account.AccountId, characterName);
    }

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

            foreach (SlotRow slot in slots)
            {
                Slots.Add(slot);
            }
        }
        catch (Exception)
        {
            // Leave empty on a read failure.
        }
    }
}
