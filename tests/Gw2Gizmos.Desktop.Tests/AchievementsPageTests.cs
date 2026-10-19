using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.Provider.Sqlite;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Platform;
using Gw2Gizmos.Desktop.Ui;
using Gw2Gizmos.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Gw2Gizmos.Desktop.Tests;

/// <summary>
/// The Achievements page's list, run headless: a row opened or closed stays where it was on screen, wherever it is in
/// the view, even though the list only builds the rows in view and estimates the rest. (With a ListBox, whose panel
/// re-placed every row from an average row height when one changed size, the opened row left the view entirely.)
/// </summary>
public sealed class AchievementsPageTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"gw2gizmos-ui-{Guid.NewGuid():N}");

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public Task An_opened_or_closed_row_stays_where_it_was() =>
        HeadlessSession.Run(() =>
        {
            AchievementsViewModel viewModel = NewViewModel(achievements: 3000);
            var window = new Window { Width = 1600, Height = 1000, Content = new AchievementsPage(viewModel) };
            window.Show();
            Until(() => viewModel.Rows.Count > 0);
            Settle();

            Control list = window.GetVisualDescendants().OfType<ItemsRepeater>()
                .Single(l => l.Name == "AchievementList");
            ScrollViewer scroller = window.GetVisualDescendants().OfType<ScrollViewer>()
                .Single(s => s.Name == "AchievementScroller");
            scroller.Offset = new Vector(0, 40_000);
            Settle();

            var moves = new List<string>();
            foreach (double at in new[] { 0.15, 0.5, 0.8 })
            {
                // One achievement near that point of the view, opened and closed twice, followed by its data (its row
                // container can be recycled while it changes).
                var achievement = (AchievementRow)RowNear(list, scroller, at).DataContext!;
                foreach (bool open in new[] { true, false, true, false })
                {
                    double before = TopOf(list, scroller, achievement);
                    achievement.IsExpanded = open;
                    Settle();
                    double moved = TopOf(list, scroller, achievement) - before;
                    moves.Add($"{(open ? "open" : "close")} at {at:P0}: {moved:+0.#;-0.#;0}px");
                }
            }

            output.WriteLine(string.Join("\n", moves));
            Assert.All(moves, move => Assert.EndsWith(": 0px", move));
            window.Close();
            return Task.CompletedTask;
        });

    /// <summary>The row whose header is nearest <paramref name="fraction"/> of the way down the view.</summary>
    private static Expander RowNear(Control list, ScrollViewer scroller, double fraction) =>
        list.GetVisualDescendants().OfType<Expander>()
            .Where(e => e.IsEffectivelyVisible && e.TranslatePoint(default, scroller) is not null)
            .MinBy(e => Math.Abs(Top(e, scroller) - scroller.Viewport.Height * fraction))!;

    /// <summary>Where the row showing <paramref name="achievement"/> starts in the view; NaN when none does.</summary>
    private static double TopOf(Control list, ScrollViewer scroller, AchievementRow achievement) =>
        list.GetVisualDescendants().OfType<Expander>()
            .Where(e => ReferenceEquals(e.DataContext, achievement) && e.IsEffectivelyVisible)
            .Select(e => Top(e, scroller))
            .Where(top => !double.IsNaN(top))
            .DefaultIfEmpty(double.NaN)
            .First();

    /// <summary>Where the row's top is in the view; NaN when it's no longer in the list (recycled).</summary>
    private static double Top(Visual row, Visual scroller) => row.TranslatePoint(default, scroller)?.Y ?? double.NaN;

    /// <summary>Lets layout, bindings and animations run to rest.</summary>
    private static void Settle()
    {
        for (int i = 0; i < 120; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// Waits on the UI thread, running its queued work, until <paramref name="condition"/> holds (the page loads on a
    /// background thread and posts back). Sleeps rather than awaits: an await here would wait on this same thread.
    /// </summary>
    private static void Until(Func<bool> condition)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        Assert.True(condition(), "Timed out waiting for the page to load.");
    }

    private AchievementsViewModel NewViewModel(int achievements)
    {
        var paths = new AppPaths(_directory);
        var services = new ServiceCollection();
        services.AddDbContext<Gw2GizmosDbContext>(o =>
            new SqliteDbProvider().Configure(o, $"Data Source={Path.Combine(_directory, "test.sqlite")}"));
        ServiceProvider provider = services.BuildServiceProvider();
        using (IServiceScope scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            db.Database.Migrate();
            Seed(db, achievements);
        }

        var keys = new FileGw2ApiKeyStore(paths, new SyncTriggerStore(), new PlainProtector());
        return new AchievementsViewModel(
            new AchievementsReader(provider.GetRequiredService<IServiceScopeFactory>()),
            new SelectedAccountService(paths, keys),
            new DataChangeMonitor(provider.GetRequiredService<IServiceScopeFactory>(), new AvaloniaUiDispatcher())
        );
    }

    /// <summary>Achievements across a few categories, with objective lists of varied length (so opened rows differ in
    /// height, as real ones do).</summary>
    private static void Seed(Gw2GizmosDbContext db, int count)
    {
        const int Categories = 6;
        int perCategory = count / Categories;
        for (int c = 0; c < Categories; c++)
        {
            int[] ids = Enumerable.Range(c * perCategory + 1, perCategory).ToArray();
            db.AchievementCategories.Add(
                new AchievementCategory { Id = c + 1, Name = $"Category {c + 1}", Order = c, AchievementIds = ids });
            foreach (int id in ids)
            {
                db.Achievements.Add(new Achievement
                {
                    Id = id,
                    Name = $"Achievement {id}",
                    Description = id % 3 == 0 ? $"A longer description for achievement {id}, to wrap." : "",
                    Requirement = "Do  things.",
                    Type = "Default",
                    Tiers = [new() { Count = 5, Points = 1 }, new() { Count = 10, Points = 5 }],
                    Bits = Enumerable.Range(0, id % 25)
                        .Select(b => new AchievementBit { Type = "Text", Text = $"Step {b}" })
                        .ToList(),
                });
            }
        }

        db.AchievementGroups.Add(new AchievementGroup
        {
            Id = "GROUP", Name = "General", Order = 0, CategoryIds = Enumerable.Range(1, Categories).ToArray(),
        });
        db.SaveChanges();
    }

    private sealed class PlainProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;

        public string? Unprotect(string protectedText) => protectedText;
    }
}
