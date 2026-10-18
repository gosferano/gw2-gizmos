using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>One map tile: a continent's floor at a zoom level, by column and row.</summary>
public readonly record struct MapTileKey(int Continent, int Floor, int Zoom, int X, int Y);

/// <summary>
/// Loads the official map tiles for the UI, over an on-disk cache (tiles change only when ArenaNet regenerates them,
/// so a downloaded tile is kept) and a bounded in-memory cache of decoded tiles, least recently used first out.
/// Downloads are deduplicated per tile and limited in number, and a request is dropped before it starts if the map
/// has since moved away from it. Tiles the server doesn't have (outside the drawn area) are remembered as missing.
/// Decoding is the UI head's job (<typeparamref name="TImage"/> is its image type).
/// </summary>
public sealed class MapTileCache<TImage>
    where TImage : class
{
    private const int MemoryCapacity = 400;
    private const int MaxConcurrentDownloads = 6;

    private readonly Func<byte[], TImage> _decode;
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _downloads = new(MaxConcurrentDownloads);
    private readonly ConcurrentDictionary<MapTileKey, Task<TImage?>> _inFlight = new();
    private readonly ConcurrentDictionary<MapTileKey, bool> _missing = new();
    private readonly object _lock = new();
    private readonly Dictionary<MapTileKey, LinkedListNode<(MapTileKey Key, TImage Image)>> _memory = new();
    private readonly LinkedList<(MapTileKey Key, TImage Image)> _recency = new();

    /// <param name="decode">Turns a downloaded tile (JPEG bytes) into the UI's image type.</param>
    public MapTileCache(string dataDirectory, Func<byte[], TImage> decode)
    {
        _decode = decode;
        _cacheDirectory = Path.Combine(dataDirectory, "TileCache");
    }

    /// <summary>The tile if it's already decoded in memory; never loads.</summary>
    public TImage? TryGet(MapTileKey key)
    {
        lock (_lock)
        {
            if (!_memory.TryGetValue(key, out var node))
            {
                return null;
            }

            _recency.Remove(node);
            _recency.AddFirst(node);
            return node.Value.Image;
        }
    }

    /// <summary>True when the server has no such tile.</summary>
    public bool IsMissing(MapTileKey key) => _missing.ContainsKey(key);

    /// <summary>
    /// Loads the tile from disk or the server into memory. Returns null when it's missing, can't be fetched right now
    /// (it's retried on the next request), or <paramref name="stillWanted"/> says it's no longer needed by the time a
    /// download slot frees up.
    /// </summary>
    public Task<TImage?> LoadAsync(MapTileKey key, string url, Func<bool> stillWanted)
    {
        if (TryGet(key) is { } cached)
        {
            return Task.FromResult<TImage?>(cached);
        }

        if (IsMissing(key))
        {
            return Task.FromResult<TImage?>(null);
        }

        return _inFlight.GetOrAdd(key, k => LoadAndForgetAsync(k, url, stillWanted));
    }

    private async Task<TImage?> LoadAndForgetAsync(MapTileKey key, string url, Func<bool> stillWanted)
    {
        try
        {
            byte[]? bytes = await ReadOrDownloadAsync(key, url, stillWanted).ConfigureAwait(false);
            if (bytes is null)
            {
                return null;
            }

            TImage image = _decode(bytes);
            Remember(key, image);
            return image;
        }
        catch (Exception)
        {
            // Offline, a broken download or an undecodable file: no tile this time, retried on the next request.
            return null;
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    private async Task<byte[]?> ReadOrDownloadAsync(MapTileKey key, string url, Func<bool> stillWanted)
    {
        string path = PathFor(key);
        if (File.Exists(path))
        {
            return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        }

        await _downloads.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!stillWanted())
            {
                return null;
            }

            using HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            {
                _missing[key] = true;
                return null;
            }

            response.EnsureSuccessStatusCode();
            byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            await File.WriteAllBytesAsync(temporary, bytes).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
            return bytes;
        }
        finally
        {
            _downloads.Release();
        }
    }

    private void Remember(MapTileKey key, TImage image)
    {
        lock (_lock)
        {
            if (_memory.TryGetValue(key, out var existing))
            {
                _recency.Remove(existing);
            }

            _memory[key] = _recency.AddFirst((key, image));
            while (_memory.Count > MemoryCapacity)
            {
                var oldest = _recency.Last!;
                _recency.RemoveLast();
                _memory.Remove(oldest.Value.Key);
            }
        }
    }

    private string PathFor(MapTileKey key) =>
        Path.Combine(
            _cacheDirectory,
            key.Continent.ToString(),
            key.Floor.ToString(),
            key.Zoom.ToString(),
            $"{key.X}_{key.Y}.jpg"
        );
}
