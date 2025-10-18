using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace D2Sharp.Internal.Caching;

/// <summary>
/// Provides in-memory caching of rendered diagrams with LRU eviction.
/// </summary>
internal sealed class RenderCache : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheOptions _cacheOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RenderCache class.
    /// </summary>
    /// <param name="options">Cache configuration options.</param>
    public RenderCache(D2WrapperOptions options)
    {
        _cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = options.CacheSize,
            ExpirationScanFrequency = TimeSpan.FromMinutes(5)
        };
        _cache = new MemoryCache(_cacheOptions);
    }

    /// <summary>
    /// Attempts to get a cached render result.
    /// </summary>
    /// <param name="script">The D2 script.</param>
    /// <param name="options">The render options.</param>
    /// <param name="result">The cached result if found.</param>
    /// <returns>True if the result was found in cache, false otherwise.</returns>
    public bool TryGet(string script, RenderOptions? options, out RenderResult? result)
    {
        if (_disposed)
        {
            result = null;
            return false;
        }

        var cacheKey = GenerateCacheKey(script, options);
        return _cache.TryGetValue(cacheKey, out result);
    }

    /// <summary>
    /// Stores a render result in the cache.
    /// </summary>
    /// <param name="script">The D2 script.</param>
    /// <param name="options">The render options.</param>
    /// <param name="result">The render result to cache.</param>
    /// <param name="expiration">The cache expiration time.</param>
    public void Set(string script, RenderOptions? options, RenderResult result, TimeSpan expiration)
    {
        if (_disposed)
        {
            return;
        }

        var cacheKey = GenerateCacheKey(script, options);
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = expiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, cacheEntryOptions);
    }

    /// <summary>
    /// Clears all cached items.
    /// </summary>
    public void Clear()
    {
        if (_disposed)
        {
            return;
        }

        _cache.Dispose();
    }

    /// <summary>
    /// Generates a cache key from the script and options.
    /// </summary>
    private static string GenerateCacheKey(string script, RenderOptions? options)
    {
        // Create a deterministic hash of script + options
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(script);

        if (options != null)
        {
            keyBuilder.Append('|');
            keyBuilder.Append(options.Layout?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.ThemeId?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.DarkThemeId?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.Sketch?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.Pad?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.Scale?.ToString() ?? "");
            keyBuilder.Append('|');
            keyBuilder.Append(options.Center?.ToString() ?? "");
        }

        var keyBytes = Encoding.UTF8.GetBytes(keyBuilder.ToString());
        var hashBytes = SHA256.HashData(keyBytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Disposes the cache and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Dispose();
            _disposed = true;
        }
    }
}
