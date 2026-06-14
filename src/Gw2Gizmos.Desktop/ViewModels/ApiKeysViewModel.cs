using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.Gw2Api.Client;
using ApiAccount = Gw2Gizmos.Gw2Api.Contract.V2.Account.Account;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the API Keys page: add a GW2 API key (validated against <c>/v2/account</c> + <c>/v2/tokeninfo</c>,
/// one key per account), see each stored key as a card (account name, key name, scopes), and remove it. The
/// worker picks up adds/removes on its next key-service fetch and syncs every account.
/// </summary>
public sealed class ApiKeysViewModel : ViewModelBase
{
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly IGw2ApiClientFactory _clientFactory;
    private readonly SelectedAccountService _selected;
    private string _apiKeyInput = "";
    private string _status = "";
    private bool _busy;

    public ApiKeysViewModel(FileGw2ApiKeyStore keyStore, IGw2ApiClientFactory clientFactory, SelectedAccountService selected)
    {
        _keyStore = keyStore;
        _clientFactory = clientFactory;
        _selected = selected;
        AddCommand = new RelayCommand(() => _ = AddAsync(), () => !_busy);
        DeleteCommand = new RelayCommand<string>(Delete);
        SelectAccountCommand = new RelayCommand<ApiKeyCard>(SelectAccount);
        LoadKeys();
    }

    public ObservableCollection<ApiKeyCard> Keys { get; } = new();

    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set => SetProperty(ref _apiKeyInput, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public ICommand AddCommand { get; }

    public ICommand DeleteCommand { get; }

    /// <summary>Makes a card's account the current global context (radio-style — only one is active).</summary>
    public ICommand SelectAccountCommand { get; }

    private void LoadKeys()
    {
        Keys.Clear();
        foreach (StoredApiKey key in _keyStore.GetStoredKeys())
        {
            Keys.Add(new ApiKeyCard(
                key.AccountId,
                key.AccountName,
                string.IsNullOrWhiteSpace(key.KeyName) ? "(unnamed key)" : key.KeyName,
                key.Permissions.Count == 0 ? "no scopes" : string.Join(", ", key.Permissions)
            ));
        }

        RefreshSelection();
    }

    private void SelectAccount(ApiKeyCard? card)
    {
        if (card is null)
        {
            return;
        }

        _selected.Select(card.AccountId, card.AccountName);
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (ApiKeyCard card in Keys)
        {
            card.IsSelected = string.Equals(card.AccountId, _selected.AccountId, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task AddAsync()
    {
        string key = ApiKeyInput.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            Status = "Paste a key first.";
            return;
        }

        _busy = true;
        Status = "Validating…";
        try
        {
            Gw2ApiClient client = _clientFactory.Create(key, Locale.English);

            ApiAccount? account = await client.V2.Account.GetBlob(CancellationToken.None);
            if (account is null || string.IsNullOrEmpty(account.Id))
            {
                Status = "Key is invalid or missing the 'account' scope.";
                return;
            }

            ApiTokenInfo? token = await client.V2.TokenInfo.GetBlob(CancellationToken.None);

            var stored = new StoredApiKey
            {
                Key = key,
                AccountId = account.Id,
                AccountName = account.Name,
                KeyName = token?.Name ?? "",
                Permissions = token?.Permissions.Select(p => p.ToString().ToLowerInvariant()).ToArray()
                    ?? Array.Empty<string>(),
            };

            if (!_keyStore.AddApiKey(stored))
            {
                Status = $"An API key for {account.Name} is already added.";
                return;
            }

            // First account added becomes the active context automatically.
            if (string.IsNullOrEmpty(_selected.AccountId))
            {
                _selected.Select(account.Id, account.Name);
            }

            ApiKeyInput = "";
            Status = $"Added {account.Name}.";
            LoadKeys();
        }
        catch (Exception)
        {
            Status = "Couldn't validate the key — check it's correct and you're online.";
        }
        finally
        {
            _busy = false;
        }
    }

    private void Delete(string? accountId)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            return;
        }

        _keyStore.RemoveApiKey(accountId);

        // If the active account was removed, fall back to the first remaining key's account.
        if (string.Equals(_selected.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyList<StoredApiKey> remaining = _keyStore.GetStoredKeys();
            if (remaining.Count > 0)
            {
                _selected.Select(remaining[0].AccountId, remaining[0].AccountName);
            }
        }

        Status = "Key removed.";
        LoadKeys();
    }
}

/// <summary>A stored API key as shown on a card: account name, the key's name, its scopes, and whether it's
/// the currently-selected account (the active radio choice).</summary>
public sealed class ApiKeyCard : ViewModelBase
{
    private bool _isSelected;

    public ApiKeyCard(string accountId, string accountName, string keyName, string permissions)
    {
        AccountId = accountId;
        AccountName = accountName;
        KeyName = keyName;
        Permissions = permissions;
    }

    public string AccountId { get; }

    public string AccountName { get; }

    public string KeyName { get; }

    public string Permissions { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
