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
