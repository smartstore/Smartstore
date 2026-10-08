#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Caches SVG payloads under canonical-address:revision keys. Revisions are not part of persistent icon addresses.
/// </summary>
public interface IIconCache
{
    /// <summary>
    /// Enumerates cached icon identities and revisions without loading or preparing SVG payloads.
    /// Uses the cache manager's backing store, including distributed entries when configured.
    /// Entries can expire during enumeration; GetAsync may subsequently return null.
    /// Historical revisions are included. This is cache inspection, not a usage history or kit inventory.
    /// </summary>
    /// <returns>Structured entries with opaque keys accepted by GetAsync and RemoveAsync.</returns>
    IAsyncEnumerable<IconCacheEntry> GetEntriesAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Gets an immutable cached payload, or null.
    /// </summary>
    /// <param name="key">A fully qualified canonical address followed by a colon and its content revision.</param>
    /// <returns>A shared immutable payload, or null on a cache miss.</returns>
    Task<IconSvg?> GetAsync(string key, CancellationToken cancelToken = default);

    /// <summary>
    /// Stores an immutable payload with a bounded lifetime.
    /// </summary>
    /// <param name="key">A fully qualified canonical address followed by a colon and its content revision.</param>
    /// <param name="svg">The immutable prepared payload to store in the cache.</param>
    Task PutAsync(string key, IconSvg svg, CancellationToken cancelToken = default);

    /// <summary>
    /// Removes a particular address and revision.
    /// </summary>
    /// <param name="key">The exact canonical-address:revision key to remove.</param>
    Task RemoveAsync(string key, CancellationToken cancelToken = default);

    /// <summary>
    /// Removes cached revisions of a library by its canonical library name.
    /// </summary>
    /// <param name="libraryName">The selector used in canonical addresses: the library's short name when configured, otherwise its system name.</param>
    Task InvalidateLibraryAsync(string libraryName, CancellationToken cancelToken = default);
}

/// <summary>
/// Describes one cached SVG revision without loading its payload.
/// </summary>
/// <param name="Key">The opaque key accepted by IIconCache.GetAsync and IIconCache.RemoveAsync.</param>
/// <param name="Address">The canonical icon identity, with name, library and variant selectors already separated.</param>
/// <param name="Revision">The complete source and preparation revision.</param>
public sealed record IconCacheEntry(string Key, IconAddress Address, string Revision);
