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
    /// <summary>Icons render small; decode at a modest width to keep memory down.</summary>
    private const int DecodePixelWidth = 32;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient = new();
    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<int, Task<ImageSource?>> _byItemId = new();

    public IconProvider(IServiceScopeFactory scopeFactory, string dataDirectory)
    {
        _scopeFactory = scopeFactory;
        _cacheDirectory = Path.Combine(dataDirectory, "IconCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>The item's icon, or null when it has none or can't be fetched. Memoized per item id.</summary>
    public Task<ImageSource?> GetIconAsync(int itemId) => _byItemId.GetOrAdd(itemId, LoadAsync);

    private async Task<ImageSource?> LoadAsync(int itemId)
    {
        try
        {
            string? url = await ResolveUrlAsync(itemId);
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

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
            // A missing or unreachable icon must never break the grid; just show nothing.
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
