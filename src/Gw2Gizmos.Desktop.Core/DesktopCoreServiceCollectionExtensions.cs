using Gw2Gizmos.Data.Worker;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Core;

public static class DesktopCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the desktop shares across UI heads: the per-user stores, the update check, the
    /// worker-config pipe host and worker launcher, event reminders, the log viewer's feed, the account/session readers and selection services,
    /// every view-model, and the in-process trading-post delivery poller.
    /// <para>
    /// The calling head must also register its implementations of the seams: <c>IUiDispatcher</c>,
    /// <c>IItemsViewFactory</c>, <c>IDialogService</c>, <c>IClipboardService</c>, <c>INavigationService</c>,
    /// <c>IToastService</c>, <c>IStartupRegistration</c> and <c>ISecretProtector</c>, plus the database provider
    /// (e.g. <c>AddGw2GizmosSqlite</c>) matching <see cref="DesktopDatabaseSettings.Provider"/>.
    /// </para>
    /// </summary>
    /// <param name="logStore">The log viewer's buffer, created before the host because the logger writes to it.</param>
    /// <param name="workerConfigPipeName">The pipe the worker process fetches its live config from.</param>
    /// <param name="mumbleLinkFilePath">Where the head's MumbleLink bridge mirrors the game's shared memory (Linux);
    /// null when the worker can read the game's shared memory itself (Windows).</param>
    public static IServiceCollection AddGw2GizmosDesktopCore(
        this IServiceCollection services,
        AppPaths paths,
        LogStore logStore,
        string workerConfigPipeName,
        DesktopDatabaseSettings database,
        string? mumbleLinkFilePath = null
    )
    {
        // The desktop owns its own state as per-user files (the worker owns the ingestion DB and the desktop
        // opens it read-only). These are registered before the engine so its TryAdd defaults are skipped.
        services.AddSingleton(paths);
        // Self-update: the periodic Velopack check (also asked whether this is an installed build) records a staged
        // update in UpdateStatus, which the dashboard's App card reads.
        services.AddSingleton<UpdateStatus>();
        services.AddSingleton<AppUpdateService>();
        services.AddHostedService(sp => sp.GetRequiredService<AppUpdateService>());
        // Per-sync trigger generations: bumped when the user enables a feature or adds a key, so the worker syncs
        // that data immediately. Registered first — the key/feature stores bump it.
        services.AddSingleton<SyncTriggerStore>();
        // Queues user "delete stored data" requests; shipped to the worker (sole DB writer) over the config pipe.
        services.AddSingleton<DeleteRequestStore>();
        // Hides sessions/segments the user deleted this run until the worker processes the delete (avoids a reappear).
        services.AddSingleton<SessionDeletionTracker>();
        services.AddSingleton<FileGw2ApiKeyStore>();
        services.AddSingleton<IGw2ApiKeyProvider>(sp => sp.GetRequiredService<FileGw2ApiKeyStore>());
        // Shared validate-and-store path for the API Keys page and first-run onboarding.
        services.AddSingleton<ApiKeyRegistrar>();
        services.AddSingleton<OnboardingStore>();
        // Worker config the desktop owns (source of truth, persisted here) and pushes to the worker over the
        // config pipe: feature on/off toggles, per-sync intervals, and the trigger generations.
        services.AddSingleton<FeatureSettingsStore>();
        services.AddSingleton<IntervalSettingsStore>();
        // Serve keys + features + intervals to the cross-platform worker over a local pipe (it never reads our
        // secret file).
        services.AddHostedService(sp => new WorkerConfigHost(
            workerConfigPipeName,
            sp.GetRequiredService<FileGw2ApiKeyStore>(),
            sp.GetRequiredService<FeatureSettingsStore>(),
            sp.GetRequiredService<IntervalSettingsStore>(),
            sp.GetRequiredService<SyncTriggerStore>(),
            sp.GetRequiredService<DeleteRequestStore>(),
            sp.GetRequiredService<ILogger<WorkerConfigHost>>()
        ));
        // Spawns the worker with the same database and this pipe's name; the head starts it once the host is up.
        services.AddSingleton(sp => new WorkerProcessLauncher(
            paths,
            database,
            workerConfigPipeName,
            sp.GetRequiredService<ILogger<WorkerProcessLauncher>>(),
            mumbleLinkFilePath
        ));
        // The in-process delivery poller persists its baseline to a file rather than the worker-owned DB.
        services.AddSingleton<IDeliveryBaselineStore, FileDeliveryBaselineStore>();

        // Notifications surface only as OS notifications (no in-app feed). A per-category global toggle on the
        // Notifications page can silence a category; the dispatcher checks it before firing.
        services.AddSingleton<NotificationSettingsStore>();
        services.AddSingleton<INotifier, NotificationDispatcher>();

        // Event reminders + Events-screen state: the per-event opt-in store, the favorites (pin-to-top) store,
        // the configurable lead-time setting, and the scheduler that notifies before a subscribed event begins.
        services.AddSingleton<EventSubscriptionStore>();
        services.AddSingleton<EventFavoritesStore>();
        services.AddSingleton<ReminderSettingsStore>();
        services.AddHostedService<EventReminderService>();

        // In-app log viewer: this process's events (the head's logger writes to logStore) + the worker's (tailed file).
        services.AddSingleton(logStore);
        services.AddHostedService<WorkerLogTailer>();

        // View-models (each head registers the pages/views that render them).
        services.AddTransient<DashboardViewModel>();
        services.AddSingleton<NotificationsViewModel>();
        // Singleton so its one-second countdown clock keeps ticking and the event list is built once.
        services.AddSingleton<EventsViewModel>();
        services.AddSingleton<LogsViewModel>();
        // Transient so the page re-reads the stored keys (and current feature state) on every navigation.
        services.AddTransient<ApiKeysViewModel>();
        // Transient so the page reflects the current toggle/permission state on every navigation.
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<StoredDataViewModel>();
        services.AddTransient<AdvancedSettingsViewModel>();
        services.AddTransient<OnboardingViewModel>();
        // Transient so the grid re-reads the worker's latest item/market data on every navigation.
        services.AddTransient<ItemsViewModel>();
        // Account: a shared read-only reader; each VM is transient so a section reloads fresh on navigation.
        services.AddSingleton<AccountReader>();
        // Carries the drilled-into account across parameterless page navigation.
        services.AddSingleton<SelectedAccountService>();
        services.AddTransient<AccountViewModel>();
        services.AddTransient<WalletViewModel>();
        services.AddTransient<MaterialStorageViewModel>();
        services.AddTransient<BankViewModel>();
        services.AddTransient<SharedInventoryViewModel>();
        // Characters: the drilled-into character (transient nav context), the hub + per-character VMs.
        services.AddSingleton<SelectedCharacterService>();
        services.AddTransient<CharactersViewModel>();
        services.AddTransient<CharacterViewModel>();
        services.AddTransient<CharacterInventoryViewModel>();
        // Sessions: the drilled-into session/segment (transient nav context), the hub + per-session/segment VMs.
        services.AddSingleton<SelectedSessionService>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<SessionViewModel>();
        services.AddTransient<SessionLootViewModel>();

        // Only the lightweight delivery poller runs in the UI process; ingestion is the worker's job. Last, so the
        // desktop's registrations above take precedence over the engine's TryAdd defaults.
        services.AddGw2GizmosDeliveryNotifications(database.DesktopConnectionString);

        return services;
    }
}
