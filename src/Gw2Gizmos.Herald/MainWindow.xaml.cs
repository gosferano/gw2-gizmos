using System.Windows;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Settings window: lets the user enter the GW2 API key the delivery poller uses. The key is stored
/// DPAPI-encrypted via <see cref="HeraldApiKeyStore"/>.
/// </summary>
public partial class MainWindow : Window
{
    private readonly HeraldApiKeyStore _apiKeyStore;

    public MainWindow(HeraldApiKeyStore apiKeyStore)
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
