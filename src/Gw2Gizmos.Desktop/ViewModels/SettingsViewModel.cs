using System.Windows.Input;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly FileGw2ApiKeyStore _apiKeyStore;
    private string _apiKeyInput = "";
    private string _status = "";

    public SettingsViewModel(FileGw2ApiKeyStore apiKeyStore)
    {
        _apiKeyStore = apiKeyStore;
        SaveCommand = new RelayCommand(Save);
        ClearCommand = new RelayCommand(Clear);
        UpdateStatus();
    }

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

    public ICommand SaveCommand { get; }

    public ICommand ClearCommand { get; }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput))
        {
            Status = "Enter a key first.";
            return;
        }

        _apiKeyStore.SetApiKey(ApiKeyInput);
        ApiKeyInput = "";
        UpdateStatus();
    }

    private void Clear()
    {
        _apiKeyStore.SetApiKey(null);
        ApiKeyInput = "";
        UpdateStatus();
    }

    private void UpdateStatus() => Status = _apiKeyStore.HasApiKey ? "API key configured" : "No API key set.";
}
