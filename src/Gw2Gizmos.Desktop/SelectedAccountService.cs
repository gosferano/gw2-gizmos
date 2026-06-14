using System;
using System.IO;
using System.Linq;
using Gw2Gizmos.Desktop.Controls;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The single source of the "current account": a global context the user picks on the API Keys page that
/// drives every account/character view (the side nav stays flat, so the account is never a navigation level
/// or a breadcrumb crumb). The choice is persisted per-user so it survives a restart; on startup it defaults
/// to the persisted account, else the first stored key's account.
/// </summary>
public sealed class SelectedAccountService
{
    private readonly string _path;

    public SelectedAccountService(AppPaths paths, FileGw2ApiKeyStore keyStore)
    {
        _path = paths.File("selected-account.txt");

        string? persisted = Load();
        var keys = keyStore.GetStoredKeys();
        StoredApiKey? chosen =
            keys.FirstOrDefault(k => string.Equals(k.AccountId, persisted, StringComparison.OrdinalIgnoreCase))
            ?? keys.FirstOrDefault();

        if (chosen is not null)
        {
            AccountId = chosen.AccountId;
            AccountName = chosen.AccountName;
        }
    }

    /// <summary>Raised when <see cref="Select"/> changes the current account.</summary>
    public event Action? CurrentAccountChanged;

    public string? AccountId { get; private set; }

    public string? AccountName { get; private set; }

    /// <summary>Makes the given account the current context (and persists it).</summary>
    public void Select(string accountId, string accountName)
    {
        if (string.Equals(AccountId, accountId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AccountId = accountId;
        AccountName = accountName;
        Save(accountId);
        CurrentAccountChanged?.Invoke();
    }

    private string? Load()
    {
        try
        {
            return File.Exists(_path) ? File.ReadAllText(_path).Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void Save(string accountId)
    {
        try
        {
            File.WriteAllText(_path, accountId);
        }
        catch (Exception)
        {
            // Best effort — losing the persisted selection only means re-picking after a restart.
        }
    }
}

/// <summary>Builds the Account breadcrumb trail. The account is global context (shown in the page header), so
/// it's never a crumb — the trail is just <c>Account › ‹section›</c>.</summary>
public static class AccountBreadcrumbs
{
    public static BreadcrumbEntry[] Section(string sectionTitle) => new[]
    {
        new BreadcrumbEntry { Title = "Account", Target = typeof(AccountPage) },
        new BreadcrumbEntry { Title = sectionTitle },
    };
}
