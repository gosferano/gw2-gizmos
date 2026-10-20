using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using FluentAvalonia.Styling;

namespace Gw2Gizmos.Desktop.Tests;

/// <summary>
/// The app's look without the app: FluentAvalonia and the app's own styles and resources, as App.axaml loads them, but
/// none of its startup (tray icon, worker, hosts). Views under test render with the same templates as in the app.
/// </summary>
public sealed class HeadlessApp : Application
{
    public override void Initialize()
    {
        // Load the app's assembly first, so its compiled XAML resolves by avares:// address.
        _ = typeof(App).Assembly;
        Styles.Add(new FluentAvaloniaTheme());
        var app = new Uri("avares://Gw2Gizmos/");
        Styles.Add(new StyleInclude(app) { Source = new Uri(app, "Themes/Styles.axaml") });
        Resources.MergedDictionaries.Add(new ResourceInclude(app) { Source = new Uri(app, "Themes/Controls.axaml") });
    }

    // Skia rather than the headless stub drawing, so text is measured with real fonts (widths can be checked).
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// One headless Avalonia session for the whole test run: Avalonia can only be initialised once per process.
/// </summary>
public static class HeadlessSession
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessApp)));

    /// <summary>Runs <paramref name="test"/> on the session's UI thread, to its end.</summary>
    public static Task Run(Func<Task> test) =>
        // Dispatch takes it as a function returning a task; unwrap so the test awaits the test itself, not its start.
        Session.Value.Dispatch(test, CancellationToken.None).Unwrap();
}
