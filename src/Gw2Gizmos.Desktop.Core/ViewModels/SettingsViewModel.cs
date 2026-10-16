using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Core.Mvvm;
using Gw2Gizmos.Desktop.Core.Platform;
using Gw2Gizmos.Desktop.Core.Ui;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Backs the Settings page: a toggle per <see cref="WorkerFeatures"/> entry. Flipping one persists to the
/// <see cref="FeatureSettingsStore"/> (which the worker picks up over the key pipe) and, when the current
/// account's key lacks a required permission, shows an inline hint mirroring the API Keys page's red chips. Where the
/// app manages its own installation (a Linux AppImage) it also offers Uninstall.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly FeatureSettingsStore _features;
    private readonly FileGw2ApiKeyStore _keyStore;
    private readonly SelectedAccountService _selected;
    private readonly IStartupRegistration _startup;
    private readonly IAppInstallation _installation;
    private readonly IDialogService _dialogs;
    private readonly IHostApplicationLifetime _lifetime;
    private bool _deleteDataOnUninstall;

    public SettingsViewModel(
        FeatureSettingsStore features,
        FileGw2ApiKeyStore keyStore,
        SelectedAccountService selected,
        IStartupRegistration startup,
        IAppInstallation installation,
        IDialogService dialogs,
        IHostApplicationLifetime lifetime
    )
    {
        _features = features;
        _keyStore = keyStore;
        _selected = selected;
        _startup = startup;
        _installation = installation;
        _dialogs = dialogs;
        _lifetime = lifetime;
        UninstallCommand = new RelayCommand(() => _ = UninstallAsync());

        foreach (WorkerFeature feature in WorkerFeatures.All)
        {
            Features.Add(new FeatureToggle(feature, _features.IsEnabled(feature.Key), OnToggled));
        }

        RefreshWarnings();
    }

    public ObservableCollection<FeatureToggle> Features { get; } = new();

    /// <summary>Whether the app launches (into the tray, spawning the worker) when the user logs in.</summary>
    public bool LaunchAtStartup
    {
        get => _startup.IsEnabled;
        set
        {
            if (value != _startup.IsEnabled)
            {
                _startup.SetEnabled(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Whether the page offers Uninstall (only where the app manages its own installation).</summary>
    public bool CanUninstall => _installation.CanUninstall;

    /// <summary>Uninstall also deletes the database, settings, logs and stored API keys.</summary>
    public bool DeleteDataOnUninstall
    {
        get => _deleteDataOnUninstall;
        set => SetProperty(ref _deleteDataOnUninstall, value);
    }

    public RelayCommand UninstallCommand { get; }

    private async Task UninstallAsync()
    {
        string data = DeleteDataOnUninstall
            ? "Your data (database, settings, logs) and stored API keys are deleted too."
            : "Your data and API keys are kept, so reinstalling picks up where you left off.";
        if (!await _dialogs.ConfirmAsync(
                "Uninstall Gw2Gizmos",
                $"Remove Gw2Gizmos from this computer? It closes, and its launcher entry, launch at login and the app "
                    + $"itself are removed.\n\n{data}"))
        {
            return;
        }

        _installation.Uninstall(DeleteDataOnUninstall);
        // The heads shut the whole app (window, tray, worker) down when the host stops.
        _lifetime.StopApplication();
    }

    private void OnToggled(FeatureToggle toggle)
    {
        _features.SetEnabled(toggle.Key, toggle.IsEnabled);
        RefreshWarnings();
    }

    private void RefreshWarnings()
    {
        IReadOnlyList<string> permissions = CurrentKeyPermissions();
        foreach (FeatureToggle toggle in Features)
        {
            IReadOnlyList<string> missing = toggle.IsEnabled
                ? WorkerFeatures.MissingPermissions(permissions, new[] { toggle.Key })
                : Array.Empty<string>();

            toggle.Warning = missing.Count == 0
                ? ""
                : $"The current account's key is missing: {string.Join(", ", missing)}";
        }
    }

    private IReadOnlyList<string> CurrentKeyPermissions()
    {
        if (_selected.AccountId is not { } accountId)
        {
            return Array.Empty<string>();
        }

        StoredApiKey? key = _keyStore
            .GetStoredKeys()
            .FirstOrDefault(k => string.Equals(k.AccountId, accountId, StringComparison.OrdinalIgnoreCase));
        return key?.Permissions ?? Array.Empty<string>();
    }
}

/// <summary>One feature row on the Settings page: its label, what it syncs, the permissions it needs, its
/// on/off state (two-way), and a hint when the current key can't satisfy it.</summary>
public sealed class FeatureToggle : ViewModelBase
{
    private readonly Action<FeatureToggle> _onToggled;
    private bool _isEnabled;
    private string _warning = "";

    public FeatureToggle(WorkerFeature feature, bool isEnabled, Action<FeatureToggle> onToggled)
    {
        Key = feature.Key;
        Display = feature.Display;
        Description = feature.Description;
        Permissions = feature.RequiredPermissions.Count == 0
            ? "Public data — no API key needed."
            : $"Needs: {string.Join(", ", feature.RequiredPermissions)}";
        _isEnabled = isEnabled;
        _onToggled = onToggled;
    }

    public string Key { get; }

    public string Display { get; }

    public string Description { get; }

    public string Permissions { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                _onToggled(this);
            }
        }
    }

    public string Warning
    {
        get => _warning;
        set
        {
            if (SetProperty(ref _warning, value))
            {
                OnPropertyChanged(nameof(HasWarning));
            }
        }
    }

    public bool HasWarning => !string.IsNullOrEmpty(_warning);
}
