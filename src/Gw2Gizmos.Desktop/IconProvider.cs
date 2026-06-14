using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Resolves and caches GW2 item icons for the UI. Given an item id it looks up the icon URL from the items
/// table, then returns the decoded image from a two-level cache: an in-memory map per item (which also
/// dedupes concurrent requests) over a persistent on-disk cache. Icons are immutable per URL, so once
/// downloaded they never need refetching. Returned images are frozen, so the same instance is safely
/// shared by every grid row and tree node bound to that item.
/// </summary>
public sealed class IconProvider
{
    /// <summary>Decode at the icons' native 64px so larger grid cells (10-per-row) stay crisp.</summary>
    private const int DecodePixelWidth = 64;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient = new();
    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<int, Task<ImageSource?>> _byItemId = new();
    private readonly ConcurrentDictionary<string, Task<ImageSource?>> _byUrl = new();

    public IconProvider(IServiceScopeFactory scopeFactory, string dataDirectory)
    {
        _scopeFactory = scopeFactory;
        _cacheDirectory = Path.Combine(dataDirectory, "IconCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>The item's icon, or null when it has none or can't be fetched. Memoized per item id.</summary>
    public Task<ImageSource?> GetIconAsync(int itemId) =>
        _byItemId.GetOrAdd(itemId, id => LoadCachingAsync(_byItemId, id, LoadAsync));

    /// <summary>An icon by its direct URL (e.g. a currency's icon, which isn't keyed by item id). Memoized.</summary>
    public Task<ImageSource?> GetIconByUrlAsync(string url) =>
        string.IsNullOrEmpty(url)
            ? Task.FromResult<ImageSource?>(null)
            : _byUrl.GetOrAdd(url, key => LoadCachingAsync(_byUrl, key, LoadFromUrlAsync));

    /// <summary>
    /// Runs a load and keeps its task in <paramref name="cache"/> so concurrent callers share one in-flight
    /// fetch per key (the same icon is downloaded and written once). A load that yields no image drops its entry,
    /// so a transient miss — offline, or an icon URL the worker hadn't synced yet — is retried the next time the
    /// icon is bound rather than cached as "no icon" for the session.
    /// </summary>
    private static async Task<ImageSource?> LoadCachingAsync<TKey>(
        ConcurrentDictionary<TKey, Task<ImageSource?>> cache,
        TKey key,
        Func<TKey, Task<ImageSource?>> load
    )
        where TKey : notnull
    {
        ImageSource? image = await load(key).ConfigureAwait(false);
        if (image is null)
        {
            cache.TryRemove(key, out _);
        }

        return image;
    }

    private async Task<ImageSource?> LoadAsync(int itemId)
    {
        // Never throw: a failed DB read (e.g. the worker mid-write) would fault the caller's async-void handler.
        // Returning null instead drops the cache entry above, so it retries.
        try
        {
            string? url = await ResolveUrlAsync(itemId);
            return string.IsNullOrEmpty(url) ? null : await GetIconByUrlAsync(url);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ImageSource?> LoadFromUrlAsync(string url)
    {
        try
        {
            string file = Path.Combine(_cacheDirectory, FileNameFor(url));
            byte[] bytes;
            if (File.Exists(file))
            {
                bytes = await File.ReadAllBytesAsync(file);
            }
            else
            {
                bytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(file, bytes);
            }

            return Decode(bytes);
        }
        catch
        {
            // A missing or unreachable icon must never break the UI; just show nothing.
            return null;
        }
    }

    private async Task<string?> ResolveUrlAsync(int itemId)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        return await dbContext
            .Items.Where(item => item.Id == itemId)
            .Select(item => item.Icon)
            .FirstOrDefaultAsync();
    }

    private static string FileNameFor(string url)
    {
        // GW2 icon URLs end in /{signature}/{fileId}.png; the file name is a stable id shared across items.
        string name = url[(url.LastIndexOf('/') + 1)..];
        return string.IsNullOrEmpty(name) ? Uri.EscapeDataString(url) : name;
    }

    private static ImageSource Decode(byte[] bytes)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = DecodePixelWidth;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
