using System.Collections;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Desktop.Core.Navigation;
using Gw2Gizmos.Desktop.Core.Platform;
using Gw2Gizmos.Desktop.Core.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gw2Gizmos.Desktop.Core.Tests;

/// <summary>
/// The shared registration must be complete once a head adds its seam implementations and a DB provider: every
/// view-model, store and hosted service has to be constructible. ValidateOnBuild checks the whole graph without
/// instantiating anything (so no database or pipe is touched).
/// </summary>
public sealed class DesktopCoreRegistrationTests : IDisposable
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-tests-{Guid.NewGuid():N}");

    [Fact]
    public void AddGw2GizmosDesktopCore_WithHeadSeams_ResolvesEveryRegistration()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var dispatcher = new InlineDispatcher();
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();

        services.AddGw2GizmosDesktopCore(
            new AppPaths(_dataDirectory),
            new LogStore(dispatcher),
            "test-pipe",
            DesktopDatabaseSettings.Resolve(configuration, Path.Combine(_dataDirectory, "test.sqlite")));

        // What a UI head supplies.
        services.AddSingleton<IUiDispatcher>(dispatcher);
        services.AddSingleton<IItemsViewFactory, StubItemsViewFactory>();
        services.AddSingleton<IDialogService, StubDialogService>();
        services.AddSingleton<IClipboardService, StubClipboardService>();
        services.AddSingleton<INavigationService, StubNavigationService>();
        services.AddSingleton<IToastService, StubToastService>();
        services.AddSingleton<IStartupRegistration, StubStartupRegistration>();
        services.AddSingleton<ISecretProtector, StubSecretProtector>();
        services.AddSingleton<IAppInstallation, InstallerManagedInstallation>();
        // Provided by the head's generic host.
        services.AddSingleton<IHostApplicationLifetime, StubHostApplicationLifetime>();
        services.AddGw2GizmosSqlite();

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public void Post(Action action) => action();

        public void Invoke(Action action) => action();

        public IUiTimer CreateTimer(TimeSpan interval, Action tick) => new StubTimer();
    }

    private sealed class StubTimer : IUiTimer
    {
        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class StubItemsViewFactory : IItemsViewFactory
    {
        public IItemsView Create(IList source) => new StubItemsView(source);

        public IItemsView CreateLiveSorted(IList source, IReadOnlyList<ItemsViewSort> sorts) => new StubItemsView(source);
    }

    private sealed class StubItemsView(IList source) : IItemsView
    {
        public Predicate<object>? Filter { get; set; }

        public void Refresh()
        {
        }

        public IEnumerator GetEnumerator() => source.GetEnumerator();
    }

    private sealed class StubDialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(false);

        public Task<bool> EditPriceAlertAsync(PriceAlertEditorViewModel editor) => Task.FromResult(false);
    }

    private sealed class StubClipboardService : IClipboardService
    {
        public Task SetTextAsync(string text) => Task.CompletedTask;
    }

    private sealed class StubNavigationService : INavigationService
    {
        public void NavigateTo(AppPage page, bool reload = false)
        {
        }
    }

    private sealed class StubToastService : IToastService
    {
        public void Show(string title, string body, string? copyText = null, string? copyLabel = null)
        {
        }
    }

    private sealed class StubStartupRegistration : IStartupRegistration
    {
        public bool IsEnabled => false;

        public void SetEnabled(bool enabled)
        {
        }
    }

    private sealed class StubSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;

        public string? Unprotect(string protectedText) => protectedText;
    }

    private sealed class StubHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
