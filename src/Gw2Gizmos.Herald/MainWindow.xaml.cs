using System.Windows;
using Gw2Gizmos.Data.Worker.Configuration;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Settings window: lets the user enter the GW2 API key the engine uses. The key is stored
/// DPAPI-encrypted in the shared database via <see cref="AppStateApiKeyStore"/>.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppStateApiKeyStore _apiKeyStore;

    public MainWindow(AppStateApiKeyStore apiKeyStore)
    {
        InitializeComponent();
        _apiKeyStore = apiKeyStore;
        UpdateStatus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        string key = ApiKeyBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            StatusText.Text = "Enter a key first.";
            return;
        }

        _apiKeyStore.SetApiKey(key);
        ApiKeyBox.Clear();
        UpdateStatus();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _apiKeyStore.SetApiKey(null);
        ApiKeyBox.Clear();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText.Text = _apiKeyStore.HasApiKey ? "API key configured ✓" : "No API key set.";
    }
}
