using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gw2Gizmos.Gw2Api.Client;
using ApiAccount = Gw2Gizmos.Gw2Api.Contract.V2.Account.Account;
using ApiTokenInfo = Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo.TokenInfo;

namespace Gw2Gizmos.Desktop;

/// <summary>The result of trying to register a pasted API key.</summary>
public enum ApiKeyRegistration
{
    /// <summary>Nothing was pasted.</summary>
    Empty,

    /// <summary>The key didn't resolve an account (wrong key, or missing the 'account' permission).</summary>
    Invalid,

    /// <summary>A key for that account is already stored.</summary>
    Duplicate,

    /// <summary>The validation call threw (bad key or no connectivity).</summary>
    Error,

    /// <summary>The key validated and was stored.</summary>
    Added,
}

/// <summary>The outcome plus the resolved account name (when one was reached), for status messages.</summary>
public sealed record ApiKeyRegistrationResult(ApiKeyRegistration Outcome, string? AccountName);

/// <summary>
/// Validates a pasted GW2 API key against <c>/v2/account</c> + <c>/v2/tokeninfo</c> and stores it (one key per
/// account, first one becomes the active context). Shared by the API Keys page and the first-run onboarding so the
/// add-a-key path is identical in both.
/// </summary>
public sealed class ApiKeyRegistrar
{
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly IGw2ApiClientFactory _clientFactory;
    private readonly SelectedAccountService _selected;

    public ApiKeyRegistrar(FileGw2ApiKeyStore keyStore, IGw2ApiClientFactory clientFactory, SelectedAccountService selected)
    {
        _keyStore = keyStore;
        _clientFactory = clientFactory;
        _selected = selected;
    }

    public async Task<ApiKeyRegistrationResult> RegisterAsync(string? apiKey, CancellationToken cancellationToken)
    {
        string key = (apiKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ApiKeyRegistrationResult(ApiKeyRegistration.Empty, null);
        }

        try
        {
            Gw2ApiClient client = _clientFactory.Create(key, Locale.English);

            ApiAccount? account = await client.V2.Account.GetBlob(cancellationToken);
            if (account is null || string.IsNullOrEmpty(account.Id))
            {
                return new ApiKeyRegistrationResult(ApiKeyRegistration.Invalid, null);
            }

            ApiTokenInfo? token = await client.V2.TokenInfo.GetBlob(cancellationToken);

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
                return new ApiKeyRegistrationResult(ApiKeyRegistration.Duplicate, account.Name);
            }

            // First account added becomes the active context automatically.
            if (string.IsNullOrEmpty(_selected.AccountId))
            {
                _selected.Select(account.Id, account.Name);
            }

            return new ApiKeyRegistrationResult(ApiKeyRegistration.Added, account.Name);
        }
        catch (Exception)
        {
            return new ApiKeyRegistrationResult(ApiKeyRegistration.Error, null);
        }
    }
}
