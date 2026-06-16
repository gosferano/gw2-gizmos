using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Gw2Gizmos.Data.Worker.Features;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the one-time first-run welcome page: explains the app in a line, walks the user through creating a GW2 API
/// key with exactly the permissions the app uses, and validates + stores the pasted key (via the shared
/// <see cref="ApiKeyRegistrar"/>). Finishing or skipping marks onboarding complete and lands on the dashboard.
/// </summary>
public sealed class OnboardingViewModel : ViewModelBase
{
    // Where GW2 players create API keys.
    private const string ApiKeyPageUrl = "https://account.arena.net/applications";

    private readonly ApiKeyRegistrar _registrar;
    private readonly OnboardingStore _store;
    private string _apiKeyInput = "";
    private string _status = "";
    private string _verifiedAccount = "";
    private bool _verified;
    private bool _busy;

    public OnboardingViewModel(ApiKeyRegistrar registrar, OnboardingStore store)
    {
        _registrar = registrar;
        _store = store;
        AddCommand = new RelayCommand(() => _ = AddAsync(), () => !_busy);
        OpenApiKeyPageCommand = new RelayCommand(() => OpenUrl(ApiKeyPageUrl));
        ContinueCommand = new RelayCommand(Finish);
        SkipCommand = new RelayCommand(Finish);

        // The distinct permissions every feature collectively needs — exactly what to tick when creating the key,
        // and nothing more (so the user grants the minimum the app actually uses).
        RecommendedPermissions = WorkerFeatures.RequiredPermissions(WorkerFeatures.All.Select(f => f.Key)).ToList();
    }

    /// <summary>The GW2 permissions to enable on the key, in catalog order (account, characters, inventories, wallet).</summary>
    public IReadOnlyList<string> RecommendedPermissions { get; }

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

    /// <summary>True once a key validated — reveals the confirmation + "Continue" button.</summary>
    public bool Verified
    {
        get => _verified;
        private set => SetProperty(ref _verified, value);
    }

    /// <summary>The connected account's name, shown in the confirmation line.</summary>
    public string VerifiedAccount
    {
        get => _verifiedAccount;
        private set => SetProperty(ref _verifiedAccount, value);
    }

    public ICommand AddCommand { get; }

    public ICommand OpenApiKeyPageCommand { get; }

    public ICommand ContinueCommand { get; }

    public ICommand SkipCommand { get; }

    private async Task AddAsync()
    {
        _busy = true;
        Status = "Validating…";
        try
        {
            ApiKeyRegistrationResult result = await _registrar.RegisterAsync(ApiKeyInput, CancellationToken.None);
            switch (result.Outcome)
            {
                case ApiKeyRegistration.Added:
                case ApiKeyRegistration.Duplicate: // already stored is fine here — the account is connected either way
                    VerifiedAccount = result.AccountName ?? "your account";
                    Verified = true;
                    Status = "";
                    break;
                case ApiKeyRegistration.Empty:
                    Status = "Paste your key first.";
                    break;
                case ApiKeyRegistration.Invalid:
                    Status = "That key didn't work — make sure you copied it fully and ticked the 'account' permission.";
                    break;
                case ApiKeyRegistration.Error:
                    Status = "Couldn't verify the key — check it's correct and that you're online.";
                    break;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void Finish()
    {
        _store.MarkCompleted();
        App.NavigateTo(typeof(DashboardPage));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.Exception)
        {
            // Opening the browser is best-effort; the user can still navigate there manually.
        }
    }
}
