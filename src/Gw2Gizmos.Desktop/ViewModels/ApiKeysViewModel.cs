using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the API Keys page: add a GW2 API key (validated against <c>/v2/account</c> + <c>/v2/tokeninfo</c>,
/// one key per account), see each stored key as a card (account name, key name, and every GW2 permission with
/// the ones an enabled feature needs but the key lacks shown in red), and remove it. The worker picks up
/// adds/removes on its next key-service fetch and syncs every account.
/// </summary>
public sealed class ApiKeysViewModel : ViewModelBase
{
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly ApiKeyRegistrar _registrar;
    private readonly SelectedAccountService _selected;
    private readonly FeatureSettingsStore _features;
    private string _apiKeyInput = "";
    private string _status = "";
    private bool _busy;

    public ApiKeysViewModel(
        FileGw2ApiKeyStore keyStore,
        ApiKeyRegistrar registrar,
        SelectedAccountService selected,
        FeatureSettingsStore features
    )
    {
        _keyStore = keyStore;
        _registrar = registrar;
        _selected = selected;
        _features = features;
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

        // Permissions an enabled feature needs but the key lacks are painted red; computed against the
        // currently-enabled features (re-read each navigation, so toggling on the Settings page is reflected).
        List<string> enabledFeatures = WorkerFeatures.All
            .Where(feature => _features.IsEnabled(feature.Key))
            .Select(feature => feature.Key)
            .ToList();

        foreach (StoredApiKey key in _keyStore.GetStoredKeys())
        {
            var have = new HashSet<string>(key.Permissions, StringComparer.OrdinalIgnoreCase);
            var requiredMissing = new HashSet<string>(
                WorkerFeatures.MissingPermissions(key.Permissions, enabledFeatures),
                StringComparer.OrdinalIgnoreCase
            );

            List<PermissionChip> chips = WorkerFeatures.AllPermissions
                .Select(permission => new PermissionChip(
                    permission,
                    have.Contains(permission),
                    requiredMissing.Contains(permission)
                ))
                .ToList();

            Keys.Add(new ApiKeyCard(
                key.AccountId,
                key.AccountName,
                string.IsNullOrWhiteSpace(key.KeyName) ? "(unnamed key)" : key.KeyName,
                chips
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
        _busy = true;
        Status = "Validating…";
        try
        {
            ApiKeyRegistrationResult result = await _registrar.RegisterAsync(ApiKeyInput, CancellationToken.None);
            Status = result.Outcome switch
            {
                ApiKeyRegistration.Empty => "Paste a key first.",
                ApiKeyRegistration.Invalid => "Key is invalid or missing the 'account' permission.",
                ApiKeyRegistration.Duplicate => $"An API key for {result.AccountName} is already added.",
                ApiKeyRegistration.Error => "Couldn't validate the key — check it's correct and you're online.",
                ApiKeyRegistration.Added => $"Added {result.AccountName}.",
                _ => Status,
            };

            if (result.Outcome == ApiKeyRegistration.Added)
            {
                ApiKeyInput = "";
                LoadKeys();
            }
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

/// <summary>A stored API key as shown on a card: account name, the key's name, the full permission set (with
/// required-but-missing ones flagged), and whether it's the currently-selected account (the active radio
/// choice).</summary>
public sealed class ApiKeyCard : ViewModelBase
{
    private bool _isSelected;

    public ApiKeyCard(string accountId, string accountName, string keyName, IReadOnlyList<PermissionChip> permissions)
    {
        AccountId = accountId;
        AccountName = accountName;
        KeyName = keyName;
        Permissions = permissions;
    }

    public string AccountId { get; }

    public string AccountName { get; }

    public string KeyName { get; }

    /// <summary>Every GW2 permission, each marked present on the key and/or required-but-missing.</summary>
    public IReadOnlyList<PermissionChip> Permissions { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>One permission tile on a key card. <paramref name="Present"/>: the key holds it.
/// <paramref name="RequiredMissing"/>: an enabled feature needs it but the key lacks it (painted red).</summary>
public sealed record PermissionChip(string Name, bool Present, bool RequiredMissing);
