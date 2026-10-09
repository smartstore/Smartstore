using System.Runtime.CompilerServices;
using Smartstore.Caching;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Uses the configured composite cache, including its distributed store when available.
/// </summary>
/// <param name="cache">The composite cache manager responsible for local and optional distributed storage.</param>
public sealed class IconCache(ICacheManager cache) : IIconCache
{
    private const string _keyPrefix = "icons:svg:";

    /// <inheritdoc />
    public async IAsyncEnumerable<IconCacheEntry> GetEntriesAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        // Inspect the backing cache on demand instead of maintaining a second index
        // that could drift after expiration, eviction or writes from other nodes.
        await foreach (var storedKey in cache.KeysAsync(BuildCacheKey("*")).WithCancellation(cancelToken))
        {
            var key = storedKey[_keyPrefix.Length..];
            // The revision can contain colons of its own. Its boundary is the first
            // colon after the canonical variant, not the last colon in the key.
            var separator = key.IndexOf(':', key.IndexOf('@') + 1);
            if (separator > 0 && separator < key.Length - 1
                && IconAddress.TryParse(key[..separator], out var address) && address.IsQualified)
            {
                yield return new IconCacheEntry(key, address, key[(separator + 1)..]);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IconSvg> GetAsync(string key, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        // IconSvg is immutable, so all levels may safely return their stored instance.
        return await cache.GetAsync<IconSvg>(BuildCacheKey(key));
    }

    /// <inheritdoc />
    public async Task PutAsync(string key, IconSvg svg, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        Guard.NotNull(svg);

        // Source changes produce new keys rather than rewriting old revisions. Expiration
        // eventually removes those obsolete entries, including from the distributed store.
        await cache.PutAsync(BuildCacheKey(key), svg, new CacheEntryOptions().ExpiresIn(TimeSpan.FromDays(7)));
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        await cache.RemoveAsync(BuildCacheKey(key));
    }

    /// <inheritdoc />
    public async Task InvalidateLibraryAsync(string libraryName, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        if (!IconAddress.IsQualifier(libraryName))
        {
            throw new ArgumentException("Invalid library name.", nameof(libraryName));
        }

        // Validate before composing a wildcard pattern; the selector must not inject wildcards.
        // Use the canonical selector supplied by the service, not a second resolution mechanism.
        await cache.RemoveByPatternAsync(BuildCacheKey(libraryName.ToLowerInvariant() + ":*"));
    }

    /// <summary>
    /// Adds the cache namespace to an icon key.
    /// </summary>
    /// <param name="key">The icon cache key.</param>
    /// <returns>The prefixed cache key.</returns>
    private static string BuildCacheKey(string key) => _keyPrefix + key;
}
